using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Application;

/// <summary>
/// Takrorlash kartalari bilan bazadagi ishlar. Jadval hisob-kitobi
/// (ReviewScheduler) va karta mazmuni (ReviewCardFactory) alohida toza
/// klasslarda — bu yerda faqat o'qish/yozish.
/// </summary>
public class ReviewService
{
    public const int DailyGoal = 10;

    private readonly IAppDbContext _db;

    public ReviewService(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Kartalarni kontekstga QO'SHADI, lekin SaveChanges qilmaydi — chaqiruvchi
    /// endpoint o'zining yozuvi (Activity) bilan birga BITTA tranzaksiyada
    /// saqlaydi: yo urinish ham, kartalar ham yoziladi, yo hech biri.
    /// Foydalanuvchida allaqachon bor ibora qayta qo'shilmaydi.
    /// Qaytaradi: nechta yangi karta qo'shildi.
    /// </summary>
    public async Task<int> AddCardsAsync(Guid userId, ActivityType? source, IReadOnlyList<NewCard> cards)
    {
        if (cards.Count == 0) return 0;

        // Bitta hisobda kartalar soni cheklangan (baza va takrorlash navbati
        // cheksiz o'smasin); to'lganda yangi kartalar jimgina qo'shilmaydi.
        var room = MaxCardsPerUser - await _db.ReviewCards.CountAsync(c => c.UserId == userId);
        if (room <= 0) return 0;

        var fronts = cards.Select(c => c.Front).ToList();
        var existing = await _db.ReviewCards
            .Where(c => c.UserId == userId && fronts.Contains(c.Front))
            .Select(c => c.Front)
            .ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var added = 0;
        foreach (var c in cards.Where(c => !existingSet.Contains(c.Front)).Take(room))
        {
            _db.ReviewCards.Add(new ReviewCard
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = c.Kind,
                Source = source,
                Front = c.Front,
                Back = c.Back,
                Note = c.Note,
                CreatedAtUtc = now,
                // Yangi karta ertaga emas, darhol "navbatda" — mashqdan
                // so'ng xatoni shu zahoti bir marta takrorlash foydali.
                DueAtUtc = now,
                Ease = ReviewDefaults.StartingEase,
            });
            added++;
        }
        return added;
    }

    /// <summary>Bitta foydalanuvchidagi kartalar chegarasi.</summary>
    public const int MaxCardsPerUser = 5000;

    public async Task<bool> IsFullAsync(Guid userId) =>
        await _db.ReviewCards.CountAsync(c => c.UserId == userId) >= MaxCardsPerUser;

    /// <summary>vocabOnly: faqat lug'at so'zlari ("So'zlarni takrorlash" tugmasi uchun).</summary>
    public Task<List<ReviewCard>> GetDueAsync(Guid userId, int limit, bool vocabOnly = false)
    {
        var now = DateTime.UtcNow;
        return Cards(userId, vocabOnly)
            .Where(c => c.DueAtUtc <= now)
            .OrderBy(c => c.DueAtUtc)
            .Take(limit)
            .ToListAsync();
    }

    public Task<List<ReviewCard>> GetRecentAsync(Guid userId, int limit, bool vocabOnly = false) =>
        Cards(userId, vocabOnly)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(limit)
            .ToListAsync();

    /// <summary>Lug'at: foydalanuvchining barcha so'z kartalari, yangilari birinchi.</summary>
    public Task<List<ReviewCard>> GetVocabAsync(Guid userId, int limit) =>
        Cards(userId, vocabOnly: true)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(limit)
            .ToListAsync();

    private IQueryable<ReviewCard> Cards(Guid userId, bool vocabOnly)
    {
        var query = _db.ReviewCards.Where(c => c.UserId == userId);
        return vocabOnly
            ? query.Where(c => c.Kind == ReviewCardKind.Word || c.Kind == ReviewCardKind.Manual)
            : query;
    }

    /// <summary>Kartani baholaydi va keyingi takrorlash vaqtini belgilaydi. Karta topilmasa — null.</summary>
    public async Task<ReviewCard?> GradeAsync(Guid userId, Guid cardId, ReviewGrade grade)
    {
        // UserId sharti muhim: boshqa foydalanuvchining kartasini ID'sini
        // bilgan holda ham baholab bo'lmasin.
        var card = await _db.ReviewCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card is null) return null;

        var now = DateTime.UtcNow;
        // Ikki marta bosish (yoki tarmoq qayta yuborgani) — kartani ikki
        // bosqich oldinga surib yubormasin: bir necha soniya ichidagi takror — e'tiborsiz.
        if (ReviewScheduler.IsDuplicateGrade(card.LastReviewedAtUtc, now)) return card;
        // Muddati kelmagan karta: jadval ham, XP (ReviewLog) ham o'zgarmaydi.
        if (ReviewScheduler.IsTooEarly(card.DueAtUtc, now)) return card;

        var next = ReviewScheduler.Next(
            new ReviewSchedule(card.Repetitions, card.IntervalDays, card.Ease, card.Lapses, card.DueAtUtc), grade, now);

        card.Repetitions = next.Repetitions;
        card.IntervalDays = next.IntervalDays;
        card.Ease = next.Ease;
        card.Lapses = next.Lapses;
        card.DueAtUtc = next.DueAtUtc;
        card.LastReviewedAtUtc = now;

        _db.ReviewLogs.Add(new ReviewLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardId = card.Id,
            Grade = (int)grade,
            ReviewedAtUtc = now,
        });
        await _db.SaveChangesAsync();
        return card;
    }

    private const int ShortStreakWindowDays = 120;
    private const int LongStreakWindowDays = 800;

    /// <summary>Seriya oynalari: avval 30 kun (ko'pchilik uchun yetadi), kerak bo'lsa 120, keyin 800.</summary>
    private static readonly int[] StreakWindows = [30, ShortStreakWindowDays, LongStreakWindowDays];

    public async Task<ReviewStats> GetStatsAsync(Guid userId, int tzOffsetMinutes)
    {
        var now = DateTime.UtcNow;

        // Navbatdagi va jami kartalar — bitta so'rovda.
        var counts = await _db.ReviewCards
            .Where(c => c.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Due = g.Count(c => c.DueAtUtc <= now) })
            .FirstOrDefaultAsync();
        var due = counts?.Due ?? 0;
        var total = counts?.Total ?? 0;

        // Takrorlash va mashq vaqtlari — bitta so'rovda (UNION ALL), oyna kichikdan kattaga o'sadi.
        // Seriya shu chegaraga yetgan kamdan-kam foydalanuvchi uchungina uzoqroq tarix o'qiladi
        // (aks holda seriya oyna chetida "to'xtab" qolardi).
        async Task<IReadOnlyList<DateTime>> TimesSince(int windowDays)
        {
            var since = now.AddDays(-windowDays);
            return await _db.ReviewLogs
                .Where(l => l.UserId == userId && l.ReviewedAtUtc >= since)
                .Select(l => l.ReviewedAtUtc)
                .Concat(_db.Activities
                    .Where(a => a.UserId == userId && a.CreatedAtUtc >= since)
                    .Select(a => a.CreatedAtUtc))
                .ToListAsync();
        }
        var streak = await ReviewScheduler.ComputeStreakProgressiveAsync(TimesSince, now, tzOffsetMinutes, StreakWindows);

        // Bugungi takrorlashlar: mahalliy kun chegaralari bo'yicha sanaladi (qatorlar tortilmaydi).
        var (dayStart, dayEnd) = ReviewScheduler.LocalDayBoundsUtc(now, tzOffsetMinutes);
        var reviewedTodayCount = await _db.ReviewLogs.CountAsync(l => l.UserId == userId && l.ReviewedAtUtc >= dayStart && l.ReviewedAtUtc < dayEnd);

        return new ReviewStats(due, total, reviewedTodayCount, DailyGoal, streak);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid cardId)
    {
        var card = await _db.ReviewCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card is null) return false;
        _db.ReviewCards.Remove(card);
        await _db.SaveChangesAsync();
        return true;
    }
}

public record ReviewStats(int Due, int Total, int ReviewedToday, int DailyGoal, int StreakDays);
