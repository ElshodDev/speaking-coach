using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

public record BankPick(PracticeItem Item, int Done, int Total, bool Repeated);
public record BankList(string Level, int Done, int Total, List<BankListItem> Items);
public record BankListItem(string Id, string Level, string Title, bool Done);
public record ComprehensionSubmitted(ComprehensionResult Result, string Passage, bool Saved, bool Capped, int NewCards);
public record HistoryItem(Guid Id, DateTime CreatedAtUtc, string PromptData, string ResponseData);

/// <summary>
/// O'qish/Tinglash mashqlari: tayyor kutubxonadan tanlash, mashqni vaqtincha saqlash, javoblarni serverda tekshirish
/// va tarixga yozish. AI'ni chaqirish va limitlar (HTTP bilan bog'liq) — Api'da.
/// </summary>
public class ComprehensionPractice(IAppDbContext db, IPracticeHistory history, ReviewService reviews)
{
    /// <summary>Javob berilmay tashlab ketilgan eski mashqlarni tozalash (alohida fon vazifasi o'rniga shu yerda — oddiy va yetarli).</summary>
    public async Task CleanStaleAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        await db.PendingExercises.Where(p => p.CreatedAtUtc < cutoff).ExecuteDeleteAsync();
    }

    /// <summary>Mashqni javob kutilayotgan holatda saqlaydi (to'g'ri javoblar serverda qoladi); id qaytaradi.</summary>
    public async Task<Guid> StoreAsync(ActivityType type, ComprehensionExercise exercise)
    {
        var pending = new PendingExercise { Id = Guid.NewGuid(), Type = type, Payload = JsonSerializer.Serialize(exercise), CreatedAtUtc = DateTime.UtcNow };
        db.PendingExercises.Add(pending);
        await db.SaveChangesAsync();
        return pending.Id;
    }

    private async Task<List<string>> DoneAsync(Guid? userId, ActivityType type, IEnumerable<string>? seen) =>
        userId is null
            ? (seen ?? []).Take(200).Select(id => PracticeLibrary.Find(type, id)?.Exercise.Title).OfType<string>().ToList()
            : await history.DoneTitlesAsync(userId.Value, type);

    /// <summary>Tayyor mashq: ko'rsatilgan id bo'yicha yoki darajadagi hali ishlanmagani. Mehmon ishlaganlari (seen) brauzerda saqlanadi.</summary>
    public async Task<BankPick?> PickAsync(ActivityType type, string level, Guid? userId, IEnumerable<string>? seen, string? bankId)
    {
        var done = await DoneAsync(userId, type, seen);
        var item = PracticeLibrary.Find(type, bankId) ?? PracticeLibrary.Next(type, level, done);
        if (item is null) return null;
        var (doneCount, total) = PracticeLibrary.Progress(type, item.Level, done);
        return new BankPick(item, doneCount, total, done.Contains(item.Exercise.Title));
    }

    /// <summary>Tanlangan darajada nechtasi bajarilgan va barcha darajalardagi ro'yxat (foydalanuvchi o'zi tanlaydi).</summary>
    public async Task<BankList> BankAsync(ActivityType type, string? level, Guid? userId)
    {
        var done = userId is null ? [] : await history.DoneTitlesAsync(userId.Value, type);
        var lv = LearnerLevel.Normalize(level);
        var (doneCount, total) = PracticeLibrary.Progress(type, lv, done);
        return new BankList(lv, doneCount, total, PracticeLibrary.For(type).Select(x => new BankListItem(x.Id, x.Level, x.Exercise.Title, done.Contains(x.Exercise.Title))).ToList());
    }

    /// <summary>
    /// Javoblarni tekshirish; xato javoblar takrorlash kartalariga aylanadi, kirgan foydalanuvchining urinishi tarixga yoziladi
    /// (kunlik XP chegarasigacha). Mashq bir marta ishlatiladi.
    /// </summary>
    public async Task<Outcome<ComprehensionSubmitted>> SubmitAsync(ActivityType type, Guid exerciseId, List<int>? answers, Guid? userId)
    {
        var pending = await db.PendingExercises.FirstOrDefaultAsync(p => p.Id == exerciseId && p.Type == type);
        if (pending is null) return Outcome<ComprehensionSubmitted>.Error("exercise.not_found", 404);

        var exercise = GeminiJson.DeserializeStrict<ComprehensionExercise>(pending.Payload);

        ComprehensionResult result;
        try
        {
            result = GeminiComprehensionService.Grade(exercise, answers ?? []);
        }
        catch (UserInputException ex)
        {
            return Outcome<ComprehensionSubmitted>.Error(ex.Key, 400, ex.Args);
        }

        // Mashq bir marta ishlatiladi: javob berildi — o'chiramiz.
        db.PendingExercises.Remove(pending);

        var newCards = 0;
        var capped = false;
        if (userId is not null)
        {
            var dayStart = TashkentTime.DayStartUtc(DateTime.UtcNow);
            var today = await db.Activities.CountAsync(a => a.UserId == userId && a.Type == type && a.CreatedAtUtc >= dayStart);
            capped = today >= XpGuard.ComprehensionPerDay;
        }
        if (userId is not null && !capped)
        {
            // Xato javob berilgan savollar takrorlash kartalariga aylanadi.
            newCards = await reviews.AddCardsAsync(userId.Value, type, ReviewCardFactory.FromWrongAnswers(exercise, result));
            db.Activities.Add(new Activity
            {
                Id = Guid.NewGuid(),
                Type = type,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                PromptData = pending.Payload,
                ResponseData = JsonSerializer.Serialize(result),
            });
        }

        // O'chirish va (bo'lsa) tarixga yozish — bitta tranzaksiyada.
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Ikki marta yuborildi (tugma ikki marta bosildi): birinchisi allaqachon saqlandi — ikkinchisi XP/kartalarni takrorlamasin.
            return Outcome<ComprehensionSubmitted>.Error("exercise.not_found", 409);
        }

        return Outcome<ComprehensionSubmitted>.Success(new ComprehensionSubmitted(result, exercise.Passage, userId is not null && !capped, capped, newCards));
    }

    /// <summary>
    /// Barcha mashq turlarining "Oldingi urinishlar" ro'yxati shu bitta funksiyadan o'tadi (bitta Activities jadvali — farq faqat Type'da).
    /// Kirmagan foydalanuvchi uchun bo'sh: hech kim boshqalarning tarixini ko'rmaydi.
    /// </summary>
    public async Task<List<HistoryItem>> RecentAsync(Guid? userId, ActivityType type) =>
        userId is null
            ? []
            : await db.Activities
                .Where(a => a.UserId == userId && a.Type == type)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(20)
                .Select(a => new HistoryItem(a.Id, a.CreatedAtUtc, a.PromptData, a.ResponseData))
                .ToListAsync();
}
