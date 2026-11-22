using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

public record DictationInput(string? Level, int Sentences, int CorrectWords, int TotalWords);

/// <summary>
/// Mashq natijalarini tarixga (Activities) yozish va o'qish: Diktant, Shadowing, Part 2, Speaking/Writing, "javobni yaxshila" keshi,
/// ravonlik grafigi. AI chaqiruvi, limitlar va HTTP — Api'da; bu yerda faqat saqlash qoidalari (kunlik chegara, takror yozuvlarga qarshi).
/// Tuzatishlar takrorlash kartalariga urinish bilan BITTA tranzaksiyada yoziladi.
/// </summary>
public class PracticeStore(IAppDbContext db, IPracticeHistory history, ReviewService reviews)
{
    // ---------------- Diktant ----------------

    /// <summary>Natijani saqlaydi. Toshkent kuniga <see cref="XpGuard.DictationsPerDay"/> tadan keyin yozilmaydi (false).</summary>
    public async Task<bool> SaveDictationAsync(Guid userId, DictationInput d)
    {
        var dayStart = TashkentTime.DayStartUtc(DateTime.UtcNow);
        var today = await db.Activities.CountAsync(a => a.UserId == userId && a.Type == ActivityType.Dictation && a.CreatedAtUtc >= dayStart);
        if (today >= XpGuard.DictationsPerDay) return false;

        db.Activities.Add(new Activity
        {
            Id = Guid.NewGuid(),
            Type = ActivityType.Dictation,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            PromptData = JsonSerializer.Serialize(new { level = LearnerLevel.Normalize(d.Level) }, Talk.Json),
            ResponseData = JsonSerializer.Serialize(new
            {
                sentences = d.Sentences,
                correctWords = d.CorrectWords,
                totalWords = d.TotalWords,
                accuracy = (int)Math.Round(100.0 * d.CorrectWords / d.TotalWords),
            }, Talk.Json),
        });
        await db.SaveChangesAsync();
        return true;
    }

    // ---------------- Shadowing ----------------

    /// <summary>Qaysi darslar bajarilgan va eng yaxshi ball.</summary>
    public async Task<List<ShadowingProgress>> ShadowingProgressAsync(Guid userId)
    {
        var rows = await db.Activities
            .Where(a => a.UserId == userId && a.Type == ActivityType.Shadowing)
            .Select(a => new { a.PromptData, a.ResponseData })
            .ToListAsync();
        return ShadowingService.ProgressOf(rows.Select(r => (r.PromptData, r.ResponseData)));
    }

    /// <summary>Dars yakunlandi (XP, seriya). Bir dars — 20 soatda bitta yozuv: qayta-qayta bosib XP yig'ib bo'lmaydi (false).</summary>
    public async Task<bool> SaveShadowingDoneAsync(Guid userId, ShadowingLesson lesson, int lines, int? average)
    {
        var since = DateTime.UtcNow.AddHours(-20);
        var recent = await db.Activities
            .Where(a => a.UserId == userId && a.Type == ActivityType.Shadowing && a.CreatedAtUtc > since)
            .Select(a => a.PromptData)
            .ToListAsync();
        // jsonb o'z formatida qaytaradi (bo'shliqlar bilan) — matn qidiruvi emas, JSON o'qiymiz.
        if (recent.Any(p => ShadowingService.LessonIdOf(p) == lesson.Id)) return false;

        db.Activities.Add(new Activity
        {
            Id = Guid.NewGuid(),
            Type = ActivityType.Shadowing,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            PromptData = JsonSerializer.Serialize(new { lessonId = lesson.Id, title = lesson.Title, level = lesson.Level, kind = lesson.Kind }),
            ResponseData = JsonSerializer.Serialize(new
            {
                lines = Math.Clamp(lines, 0, lesson.Lines.Count),
                total = lesson.Lines.Count,
                average = average is int a ? Math.Clamp(a, 0, 100) : (int?)null,
            }),
        });
        await db.SaveChangesAsync();
        return true;
    }

    // ---------------- Part 2 ----------------

    /// <summary>Foydalanuvchi bajargan Part 2 kartochkalari ID'lari.</summary>
    public async Task<HashSet<string>> Part2DoneAsync(Guid userId)
    {
        var done = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in await history.Part2PromptsAsync(userId))
            if (Part2Rules.CardIdOf(p) is { } id) done.Add(id);
        return done;
    }

    /// <summary>Part 2 natijasi Speaking mashqi sifatida saqlanadi (PromptData'da "part2": true) — tarix, XP, reja va kartalar alohida ishlovsiz ishlaydi.</summary>
    public Task<int> SavePart2Async(Guid userId, Guid submissionId, Part2Card card, string responseData, IReadOnlyList<CorrectionItem> corrections) =>
        SaveAttemptAsync(new Activity
        {
            Id = submissionId,
            Type = ActivityType.Speaking,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            // "topic" — Speaking tarixida mavzu shu maydondan ko'rsatiladi.
            PromptData = JsonSerializer.Serialize(new { topic = card.Topic, part2 = true, cardId = card.Id }),
            ResponseData = responseData,
        }, corrections);

    // ---------------- Speaking / Writing ----------------

    /// <summary>Urinishni tarixga yozadi; tuzatishlar bir tranzaksiyada takrorlash kartalariga aylanadi. Yangi kartalar sonini qaytaradi.</summary>
    public async Task<int> SaveAttemptAsync(Activity attempt, IReadOnlyList<CorrectionItem> corrections)
    {
        db.Activities.Add(attempt);
        var newCards = await reviews.AddCardsAsync(attempt.UserId!.Value, attempt.Type, ReviewCardFactory.FromCorrections(corrections));
        await db.SaveChangesAsync();
        return newCards;
    }

    /// <summary>Ravonlik grafigi: oxirgi urinishlardagi o'lchovlar, eski → yangi (o'lchovsizlari tashlanadi).</summary>
    public async Task<List<FluencyPoint>> FluencyAsync(Guid userId, int take) =>
        (await history.FluencyRowsAsync(userId, Math.Clamp(take, 1, 50)))
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => (r.CreatedAtUtc, r.Type, M: FluencyMetrics.FromResponse(r.ResponseData)))
            .Where(x => x.M?.Wpm is not null)
            .Select(x => new FluencyPoint(x.CreatedAtUtc, x.M!.Wpm, x.M.FillersPerMinute, x.M.LongPauses,
                x.Type switch { ActivityType.MockExam => "mock", ActivityType.Conversation => "talk", _ => "speaking" }))
            .ToList();

    // ---------------- "Javobimni yaxshila" ----------------

    /// <summary>Faqat foydalanuvchining o'z Speaking/Writing urinishi (egasi tekshiriladi).</summary>
    public Task<Activity?> OwnAttemptAsync(Guid id, Guid userId) =>
        db.Activities.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && (a.Type == ActivityType.Speaking || a.Type == ActivityType.Writing));

    /// <summary>Yaxshilangan javobni urinish ichida keshlaydi — qayta ochilganda AI limiti sarflanmaydi.</summary>
    public async Task SaveUpgradeAsync(Activity attempt, string responseData)
    {
        attempt.ResponseData = responseData;
        await db.SaveChangesAsync();
    }
}
