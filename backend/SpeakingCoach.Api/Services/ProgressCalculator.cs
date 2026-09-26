using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>Bitta mashq haqida XP va nishonlar uchun kerakli faktlar.</summary>
/// <param name="CorrectAnswers">Faqat O'qish/Tinglash uchun: nechta to'g'ri javob.</param>
public record ActivityFact(ActivityType Type, DateTime CreatedAtUtc, int? CorrectAnswers = null);

public record Badge(string Id, string Emoji, string Title, string Description, bool Earned);

/// <summary>Daraja: joriy daraja, umumiy XP va shu daraja oralig'i (progress chizig'i uchun).</summary>
public record LevelInfo(int Level, int Xp, int LevelStartXp, int NextLevelXp);

public record LeaderboardRow(Guid UserId, string Name, int Xp);
public record RankedRow(int Rank, Guid UserId, string Name, int Xp);

/// <summary>
/// O'yinlashtirish (gamification) qoidalari: XP, darajalar, nishonlar,
/// faollik kalendari va reyting. Hammasi bazada ALOHIDA saqlanmaydi —
/// mavjud yozuvlardan (Activities, ReviewLogs) hisoblanadi. Afzalligi:
/// qoidani o'zgartirsak (masalan XP miqdori), eski natijalar ham avtomatik
/// qayta hisoblanadi, "noto'g'ri saqlangan XP" degan muammo bo'lmaydi.
/// Toza funksiyalar — to'liq unit test qilingan.
/// </summary>
public static class ProgressCalculator
{
    public const int SpeakingWritingXp = 20;
    public const int ComprehensionXp = 15;
    public const int PerCorrectAnswerXp = 5;
    public const int ReviewXp = 2;
    public const int MockExamXp = 50;

    public static int XpFor(ActivityFact a) => a.Type switch
    {
        ActivityType.Speaking or ActivityType.Writing => SpeakingWritingXp,
        ActivityType.Reading or ActivityType.Listening => ComprehensionXp + PerCorrectAnswerXp * (a.CorrectAnswers ?? 0),
        ActivityType.MockExam => MockExamXp,
        _ => 0,
    };

    public static int TotalXp(IEnumerable<ActivityFact> activities, int reviewCount) =>
        activities.Sum(XpFor) + reviewCount * ReviewXp;

    /// <summary>
    /// N-darajaning boshlanishi: 50·N·(N−1) XP → 1: 0, 2: 100, 3: 300, 4: 600,
    /// 5: 1000... Har keyingi daraja oldingisidan 100 XP ko'proq talab qiladi —
    /// boshida tez o'sish (motivatsiya), keyin sekinroq.
    /// </summary>
    public static int LevelStart(int level) => 50 * level * (level - 1);

    public static LevelInfo LevelFor(int xp)
    {
        var level = 1;
        while (LevelStart(level + 1) <= xp) level++;
        return new LevelInfo(level, xp, LevelStart(level), LevelStart(level + 1));
    }

    /// <summary>Eng uzun ketma-ket kunlar zanjiri (joriy emas — hamma vaqtdagi eng yaxshisi).</summary>
    public static int BestStreak(IEnumerable<DateOnly> days)
    {
        var sorted = days.Distinct().OrderBy(d => d).ToList();
        int best = 0, run = 0;
        DateOnly? prev = null;
        foreach (var d in sorted)
        {
            run = prev is not null && d == prev.Value.AddDays(1) ? run + 1 : 1;
            best = Math.Max(best, run);
            prev = d;
        }
        return best;
    }

    public static IReadOnlyList<Badge> Badges(IReadOnlyList<ActivityFact> activities, int reviewCount, int bestStreak)
    {
        int Count(ActivityType t) => activities.Count(a => a.Type == t);
        // "To'rtala mashq turi" — faqat asosiy 4 tur (mock imtihon hisoblanmaydi).
        var kinds = activities.Select(a => a.Type).Where(t => t <= ActivityType.Listening).Distinct().Count();
        var perfect = activities.Any(a => a.CorrectAnswers == GeminiComprehensionService.QuestionCount);

        return new List<Badge>
        {
            new("first_step", "🌱", "Birinchi qadam", "Birinchi mashqni bajaring", activities.Count >= 1),
            new("all_rounder", "🧭", "Har tomonlama", "To'rtala mashq turini sinab ko'ring", kinds >= 4),
            new("streak_3", "🔥", "3 kun ketma-ket", "3 kun uzluksiz mashq qiling", bestStreak >= 3),
            new("streak_7", "⚡", "Bir hafta", "7 kun uzluksiz mashq qiling", bestStreak >= 7),
            new("streak_30", "🏆", "Bir oy", "30 kun uzluksiz mashq qiling", bestStreak >= 30),
            new("perfect", "🎯", "Mukammal natija", "O'qish yoki Tinglashda 4/4 oling", perfect),
            new("speaker_10", "🎙", "Notiq", "10 marta Gapirish mashqini bajaring", Count(ActivityType.Speaking) >= 10),
            new("writer_10", "✍️", "Yozuvchi", "10 ta insho yozing", Count(ActivityType.Writing) >= 10),
            new("reviewer_50", "🔁", "Takrorlovchi", "50 marta karta takrorlang", reviewCount >= 50),
            new("reviewer_500", "🧠", "Xotira ustasi", "500 marta karta takrorlang", reviewCount >= 500),
        };
    }

    /// <summary>
    /// Faollik kalendari (GitHub'dagi kabi): oxirgi <paramref name="days"/> kun,
    /// har kun uchun nechta harakat (mashq + takrorlash). Kunlar mahalliy
    /// vaqt bo'yicha; bo'sh kunlar ham 0 bilan qaytadi — frontend to'liq
    /// panjara chizadi.
    /// </summary>
    public static List<(DateOnly Day, int Count)> Calendar(IEnumerable<DateTime> eventsUtc, DateTime nowUtc, int tzOffsetMinutes, int days = 84)
    {
        var counts = eventsUtc
            .GroupBy(t => ReviewScheduler.ToLocalDate(t, tzOffsetMinutes))
            .ToDictionary(g => g.Key, g => g.Count());
        var today = ReviewScheduler.ToLocalDate(nowUtc, tzOffsetMinutes);
        var result = new List<(DateOnly, int)>(days);
        for (var i = days - 1; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            result.Add((d, counts.GetValueOrDefault(d)));
        }
        return result;
    }

    /// <summary>Haftaning boshi — dushanba 00:00 UTC. Musobaqa hamma uchun bir xil vaqtda yangilanadi.</summary>
    public static DateTime WeekStartUtc(DateTime nowUtc)
    {
        var daysSinceMonday = ((int)nowUtc.DayOfWeek + 6) % 7;
        return nowUtc.Date.AddDays(-daysSinceMonday);
    }

    /// <summary>
    /// Reyting: XP bo'yicha kamayish tartibida. Teng XP — teng o'rin
    /// ("1, 1, 3" — sport musobaqalaridagi kabi). 0 XP'lilar ko'rsatilmaydi.
    /// </summary>
    public static List<RankedRow> Rank(IEnumerable<LeaderboardRow> rows)
    {
        var ordered = rows.Where(r => r.Xp > 0)
            .OrderByDescending(r => r.Xp)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var result = new List<RankedRow>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var rank = i > 0 && ordered[i].Xp == ordered[i - 1].Xp ? result[i - 1].Rank : i + 1;
            result.Add(new RankedRow(rank, ordered[i].UserId, ordered[i].Name, ordered[i].Xp));
        }
        return result;
    }

    /// <summary>Admin panelda email to'liq ko'rsatilmaydi: "aziza@mail.com" → "az***@mail.com".</summary>
    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***{email[at..]}";
    }

    /// <summary>O'qish/Tinglash natijasidan to'g'ri javoblar sonini o'qiydi (ResponseData JSON).</summary>
    public static int? ReadCorrectAnswers(ActivityType type, string responseJson)
    {
        if (type is not (ActivityType.Reading or ActivityType.Listening)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
            return doc.RootElement.TryGetProperty("score", out var s) && s.TryGetInt32(out var v) ? v : 0;
        }
        catch (System.Text.Json.JsonException)
        {
            return 0;
        }
    }
}
