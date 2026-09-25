using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

public record CalendarDay(string Date, int Count);
public record TrendPoint(DateTime At, Dictionary<string, int> Scores);
public record HardCard(string Front, string Back, int Lapses);

public record ProgressDto(
    LevelInfo Level,
    int BestStreak,
    int TotalActivities,
    int TotalReviews,
    Dictionary<string, int> ActivitiesByType,
    List<CalendarDay> Calendar,
    List<TrendPoint> SpeakingTrend,
    List<TrendPoint> WritingTrend,
    IReadOnlyList<Badge> Badges,
    List<HardCard> HardestCards);

public record LeaderboardEntry(int Rank, string Name, int Xp, bool IsMe);
public record LeaderboardDto(DateTime WeekStartUtc, List<LeaderboardEntry> Entries, int MyXp, int? MyRank, bool OptedIn, bool HasDisplayName);

/// <summary>
/// Progress sahifasi va haftalik musobaqa uchun ma'lumotlarni bazadan
/// yig'adi. Hisob-kitob qoidalari ProgressCalculator'da (toza, testlangan);
/// bu yerda faqat so'rovlar.
/// </summary>
public class ProgressService
{
    private const int TrendLength = 20;
    private readonly AppDbContext _db;

    public ProgressService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProgressDto> GetProgressAsync(Guid userId, int tzOffsetMinutes)
    {
        var now = DateTime.UtcNow;

        // Faqat O'qish/Tinglash javoblari kerak (XP'dagi to'g'ri javob bonusi
        // uchun). Qolganlari uchun katta JSON'ni bazadan tortib o'tirmaymiz —
        // CASE ifodasi SQL'ning o'zida bajariladi.
        var activities = await _db.Activities
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.Type,
                a.CreatedAtUtc,
                Response = a.Type == ActivityType.Reading || a.Type == ActivityType.Listening ? a.ResponseData : null,
            })
            .ToListAsync();

        var facts = activities
            .Select(a => new ActivityFact(a.Type, a.CreatedAtUtc,
                a.Response is null ? null : ProgressCalculator.ReadCorrectAnswers(a.Type, a.Response)))
            .ToList();

        var reviewTimes = await _db.ReviewLogs
            .Where(l => l.UserId == userId)
            .Select(l => l.ReviewedAtUtc)
            .ToListAsync();

        var allTimes = facts.Select(f => f.CreatedAtUtc).Concat(reviewTimes).ToList();
        var bestStreak = ProgressCalculator.BestStreak(
            allTimes.Select(t => ReviewScheduler.ToLocalDate(t, tzOffsetMinutes)));

        var calendar = ProgressCalculator.Calendar(allTimes, now, tzOffsetMinutes)
            .Select(d => new CalendarDay(d.Day.ToString("yyyy-MM-dd"), d.Count))
            .ToList();

        var hardest = await _db.ReviewCards
            .Where(c => c.UserId == userId && c.Lapses > 0)
            .OrderByDescending(c => c.Lapses)
            .Take(5)
            .Select(c => new HardCard(c.Front, c.Back, c.Lapses))
            .ToListAsync();

        return new ProgressDto(
            ProgressCalculator.LevelFor(ProgressCalculator.TotalXp(facts, reviewTimes.Count)),
            bestStreak,
            facts.Count,
            reviewTimes.Count,
            facts.GroupBy(f => f.Type.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            calendar,
            await TrendAsync(userId, ActivityType.Speaking, "fluency", "grammar", "vocabulary"),
            await TrendAsync(userId, ActivityType.Writing, "taskAchievement", "coherenceCohesion", "grammar", "vocabulary"),
            ProgressCalculator.Badges(facts, reviewTimes.Count, bestStreak),
            hardest);
    }

    /// <summary>Oxirgi 20 ta urinishdagi ballar — eski → yangi tartibida (grafik uchun).</summary>
    private async Task<List<TrendPoint>> TrendAsync(Guid userId, ActivityType type, params string[] criteria)
    {
        var rows = await _db.Activities
            .Where(a => a.UserId == userId && a.Type == type)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(TrendLength)
            .Select(a => new { a.CreatedAtUtc, a.ResponseData })
            .ToListAsync();

        var points = new List<TrendPoint>();
        foreach (var r in rows.OrderBy(r => r.CreatedAtUtc))
        {
            try
            {
                using var doc = JsonDocument.Parse(r.ResponseData);
                var scores = new Dictionary<string, int>();
                foreach (var c in criteria)
                {
                    if (doc.RootElement.TryGetProperty(c, out var el) && el.TryGetProperty("score", out var s) && s.TryGetInt32(out var v))
                    {
                        scores[c] = v;
                    }
                }
                if (scores.Count == criteria.Length) points.Add(new TrendPoint(r.CreatedAtUtc, scores));
            }
            catch (JsonException)
            {
                // Eski yoki buzilgan yozuv — grafikdan tushirib qoldiramiz.
            }
        }
        return points;
    }

    public async Task<LeaderboardDto> GetLeaderboardAsync(Guid meId)
    {
        var weekStart = ProgressCalculator.WeekStartUtc(DateTime.UtcNow);

        var me = await _db.Users.FirstAsync(u => u.Id == meId);
        var participants = await _db.Users
            .Where(u => u.ShowOnLeaderboard && u.DisplayName != null)
            .Select(u => new { u.Id, Name = u.DisplayName! })
            .ToListAsync();
        var ids = participants.Select(p => p.Id).Append(meId).Distinct().ToList();

        var acts = await _db.Activities
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value) && a.CreatedAtUtc >= weekStart)
            .Select(a => new
            {
                UserId = a.UserId!.Value,
                a.Type,
                a.CreatedAtUtc,
                Response = a.Type == ActivityType.Reading || a.Type == ActivityType.Listening ? a.ResponseData : null,
            })
            .ToListAsync();
        var reviews = await _db.ReviewLogs
            .Where(l => ids.Contains(l.UserId) && l.ReviewedAtUtc >= weekStart)
            .GroupBy(l => l.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        int WeeklyXp(Guid id) => ProgressCalculator.TotalXp(
            acts.Where(a => a.UserId == id).Select(a => new ActivityFact(a.Type, a.CreatedAtUtc,
                a.Response is null ? null : ProgressCalculator.ReadCorrectAnswers(a.Type, a.Response))),
            reviews.FirstOrDefault(r => r.UserId == id)?.Count ?? 0);

        var ranked = ProgressCalculator.Rank(participants.Select(p => new LeaderboardRow(p.Id, p.Name, WeeklyXp(p.Id))));
        var mine = ranked.FirstOrDefault(r => r.UserId == meId);

        return new LeaderboardDto(
            weekStart,
            ranked.Take(20).Select(r => new LeaderboardEntry(r.Rank, r.Name, r.Xp, r.UserId == meId)).ToList(),
            WeeklyXp(meId),
            mine?.Rank,
            me.ShowOnLeaderboard,
            !string.IsNullOrWhiteSpace(me.DisplayName));
    }
}
