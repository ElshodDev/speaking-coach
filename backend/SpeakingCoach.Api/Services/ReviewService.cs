using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Takrorlash kartalari bilan bazadagi ishlar. Jadval hisob-kitobi
/// (ReviewScheduler) va karta mazmuni (ReviewCardFactory) alohida toza
/// klasslarda — bu yerda faqat o'qish/yozish.
/// </summary>
public class ReviewService
{
    public const int DailyGoal = 10;

    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
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

        var fronts = cards.Select(c => c.Front).ToList();
        var existing = await _db.ReviewCards
            .Where(c => c.UserId == userId && fronts.Contains(c.Front))
            .Select(c => c.Front)
            .ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var added = 0;
        foreach (var c in cards.Where(c => !existingSet.Contains(c.Front)))
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

    public Task<List<ReviewCard>> GetDueAsync(Guid userId, int limit)
    {
        var now = DateTime.UtcNow;
        return _db.ReviewCards
            .Where(c => c.UserId == userId && c.DueAtUtc <= now)
            .OrderBy(c => c.DueAtUtc)
            .Take(limit)
            .ToListAsync();
    }

    public Task<List<ReviewCard>> GetRecentAsync(Guid userId, int limit) =>
        _db.ReviewCards
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(limit)
            .ToListAsync();

    /// <summary>Kartani baholaydi va keyingi takrorlash vaqtini belgilaydi. Karta topilmasa — null.</summary>
    public async Task<ReviewCard?> GradeAsync(Guid userId, Guid cardId, ReviewGrade grade)
    {
        // UserId sharti muhim: boshqa foydalanuvchining kartasini ID'sini
        // bilgan holda ham baholab bo'lmasin.
        var card = await _db.ReviewCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card is null) return null;

        var now = DateTime.UtcNow;
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

    public async Task<ReviewStats> GetStatsAsync(Guid userId, int tzOffsetMinutes)
    {
        var now = DateTime.UtcNow;
        var since = now.AddDays(-120);

        var due = await _db.ReviewCards.CountAsync(c => c.UserId == userId && c.DueAtUtc <= now);
        var total = await _db.ReviewCards.CountAsync(c => c.UserId == userId);

        var reviewTimes = await _db.ReviewLogs
            .Where(l => l.UserId == userId && l.ReviewedAtUtc >= since)
            .Select(l => l.ReviewedAtUtc)
            .ToListAsync();
        var activityTimes = await _db.Activities
            .Where(a => a.UserId == userId && a.CreatedAtUtc >= since)
            .Select(a => a.CreatedAtUtc)
            .ToListAsync();

        var today = ReviewScheduler.ToLocalDate(now, tzOffsetMinutes);
        var reviewedToday = reviewTimes.Count(t => ReviewScheduler.ToLocalDate(t, tzOffsetMinutes) == today);

        // Streak'ga takrorlash HAM, istalgan mashq HAM kiradi — maqsad har kuni
        // ozgina bo'lsa-da ingliz tili bilan shug'ullanish.
        var streak = ReviewScheduler.ComputeStreak(reviewTimes.Concat(activityTimes), now, tzOffsetMinutes);

        return new ReviewStats(due, total, reviewedToday, DailyGoal, streak);
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
