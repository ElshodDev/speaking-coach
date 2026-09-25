using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Kim admin — konfiguratsiyadan: "Admin:Emails" (vergul bilan ajratilgan).
/// Render'da: Admin__Emails environment variable. Bazada "IsAdmin" ustuni
/// yo'q — adminni faqat server egasi belgilaydi, hech bir API orqali o'zini
/// admin qilib bo'lmaydi.
/// </summary>
public class AdminOptions
{
    private readonly HashSet<string> _emails;

    public AdminOptions(IConfiguration config)
    {
        _emails = (config["Admin:Emails"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(AuthService.NormalizeEmail)
            .ToHashSet();
    }

    public bool IsAdmin(User? user) => user is not null && _emails.Contains(user.Email);
}

public record DailyPoint(string Date, int Signups, int ActiveUsers, int Activities, int Reviews);
public record RecentUser(string Email, DateTime CreatedAtUtc, DateTime? LastActiveAtUtc, int Activities, string Level);

public record AdminOverview(
    int TotalUsers,
    int NewUsers7d,
    int ActiveToday,
    int Active7d,
    int Active30d,
    Dictionary<string, int> ActivitiesByType,
    Dictionary<string, int> ActivitiesByType7d,
    int TotalReviews,
    int Reviews7d,
    int TotalCards,
    List<DailyPoint> Daily,
    List<RecentUser> RecentUsers);

/// <summary>
/// Admin panel uchun umumiy statistika. Faqat SONLAR va niqoblangan
/// emaillar — foydalanuvchilarning insholari, audio transkriptlari yoki
/// kartalari admin'ga ham ko'rsatilmaydi (shaxsiy ma'lumot minimal).
/// </summary>
public class AdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminOverview> GetOverviewAsync(int tzOffsetMinutes)
    {
        var now = DateTime.UtcNow;
        var since30 = now.AddDays(-30);
        var since7 = now.AddDays(-7);
        DateOnly Local(DateTime t) => ReviewScheduler.ToLocalDate(t, tzOffsetMinutes);
        var today = Local(now);

        var users = await _db.Users.Select(u => new { u.Id, u.Email, u.CreatedAtUtc, u.Level }).ToListAsync();

        // Oxirgi 30 kundagi barcha "harakatlar" (mashq yoki takrorlash) —
        // kim, qachon. Faol foydalanuvchilar va kunlik grafik shundan.
        var recentActs = await _db.Activities
            .Where(a => a.UserId != null && a.CreatedAtUtc >= since30)
            .Select(a => new { UserId = a.UserId!.Value, a.CreatedAtUtc, a.Type })
            .ToListAsync();
        var recentReviews = await _db.ReviewLogs
            .Where(l => l.ReviewedAtUtc >= since30)
            .Select(l => new { l.UserId, l.ReviewedAtUtc })
            .ToListAsync();
        var events = recentActs.Select(a => (a.UserId, At: a.CreatedAtUtc))
            .Concat(recentReviews.Select(r => (r.UserId, At: r.ReviewedAtUtc)))
            .ToList();

        int ActiveSince(Func<DateTime, bool> inWindow) => events.Where(e => inWindow(e.At)).Select(e => e.UserId).Distinct().Count();

        var byType = await _db.Activities
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var daily = new List<DailyPoint>();
        for (var i = 29; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            daily.Add(new DailyPoint(
                d.ToString("yyyy-MM-dd"),
                users.Count(u => Local(u.CreatedAtUtc) == d),
                events.Where(e => Local(e.At) == d).Select(e => e.UserId).Distinct().Count(),
                recentActs.Count(a => Local(a.CreatedAtUtc) == d),
                recentReviews.Count(r => Local(r.ReviewedAtUtc) == d)));
        }

        var recent = users.OrderByDescending(u => u.CreatedAtUtc).Take(10).ToList();
        var recentIds = recent.Select(u => u.Id).ToList();
        var lastActivity = await _db.Activities
            .Where(a => a.UserId != null && recentIds.Contains(a.UserId.Value))
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Last = g.Max(a => a.CreatedAtUtc) })
            .ToListAsync();
        var lastReview = await _db.ReviewLogs
            .Where(l => recentIds.Contains(l.UserId))
            .GroupBy(l => l.UserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(l => l.ReviewedAtUtc) })
            .ToListAsync();

        var recentUsers = recent.Select(u =>
        {
            var a = lastActivity.FirstOrDefault(x => x.UserId == u.Id);
            var r = lastReview.FirstOrDefault(x => x.UserId == u.Id);
            DateTime? last = new[] { a?.Last, r?.Last }.Where(x => x is not null).Max();
            return new RecentUser(ProgressCalculator.MaskEmail(u.Email), u.CreatedAtUtc, last, a?.Count ?? 0, u.Level);
        }).ToList();

        return new AdminOverview(
            users.Count,
            users.Count(u => u.CreatedAtUtc >= since7),
            ActiveSince(t => Local(t) == today),
            ActiveSince(t => t >= since7),
            ActiveSince(_ => true),
            byType.ToDictionary(x => x.Type.ToString(), x => x.Count),
            recentActs.Where(a => a.CreatedAtUtc >= since7).GroupBy(a => a.Type).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            await _db.ReviewLogs.CountAsync(_ => true),
            recentReviews.Count(r => r.ReviewedAtUtc >= since7),
            await _db.ReviewCards.CountAsync(_ => true),
            daily,
            recentUsers);
    }
}
