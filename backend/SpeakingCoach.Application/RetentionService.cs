using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

public record RetentionReport(
    int Days,
    string Definition,
    RetentionSummary Summary,
    decimal? D1Percent,
    decimal? D7Percent,
    List<RetentionCohort> Cohorts);

/// <summary>
/// Retention hisoboti mavjud jadvallardan (sxema o'zgarmaydi): Users.CreatedAtUtc — ro'yxatdan o'tish,
/// Activities va ReviewLogs — faollik. Demo hisoblar hisobga olinmaydi. Faqat admin uchun.
/// </summary>
public class RetentionService(IAppDbContext db)
{
    public const int MaxDays = 90;
    public const int DefaultDays = 30;

    public async Task<RetentionReport> GetAsync(int days, DateTime nowUtc, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, MaxDays);
        var today = TashkentTime.Today(nowUtc);
        // Guruh sanalari Toshkent kuni bilan; boshlang'ich chegara — eng eski kunning Toshkent yarim tuni.
        var sinceUtc = TashkentTime.DayStartUtc(nowUtc).AddDays(-(days - 1));

        var users = (await db.Users
                .Where(u => u.CreatedAtUtc >= sinceUtc)
                .Select(u => new { u.Id, u.Email, u.CreatedAtUtc })
                .ToListAsync(ct))
            .Where(u => !DemoAccount.IsDemo(u.Email))
            .Select(u => (UserId: u.Id, SignupDay: TashkentTime.Today(u.CreatedAtUtc)))
            .ToList();

        // Faol kunlar: kun bazaning o'zida Toshkent vaqti (UTC+5) bo'yicha kesiladi va takrorlar olib tashlanadi —
        // har bir (foydalanuvchi, kun) juftligi bir qatordan keladi.
        var hours = TashkentTime.Offset.TotalHours;
        var fromActivities = await db.Activities
            .Where(a => a.UserId != null && a.CreatedAtUtc >= sinceUtc && db.Users.Any(u => u.Id == a.UserId && u.CreatedAtUtc >= sinceUtc))
            .Select(a => new { UserId = a.UserId!.Value, Day = a.CreatedAtUtc.AddHours(hours).Date })
            .Distinct()
            .ToListAsync(ct);
        var fromReviews = await db.ReviewLogs
            .Where(r => r.ReviewedAtUtc >= sinceUtc && db.Users.Any(u => u.Id == r.UserId && u.CreatedAtUtc >= sinceUtc))
            .Select(r => new { r.UserId, Day = r.ReviewedAtUtc.AddHours(hours).Date })
            .Distinct()
            .ToListAsync(ct);

        var active = new Dictionary<Guid, HashSet<DateOnly>>();
        foreach (var x in fromActivities.Select(a => (a.UserId, a.Day)).Concat(fromReviews.Select(r => (r.UserId, r.Day))))
        {
            if (!active.TryGetValue(x.Item1, out var set)) active[x.Item1] = set = [];
            set.Add(DateOnly.FromDateTime(x.Item2));
        }

        var cohorts = RetentionCalculator.Cohorts(users, active, today);
        var summary = RetentionCalculator.Summarize(cohorts);
        return new RetentionReport(
            days,
            "N-kun retention: ro'yxatdan o'tgan kundan keyin aynan N-kuni (Toshkent vaqti) mashq qilgan yoki karta takrorlagan foydalanuvchilar ulushi. Demo hisoblar hisobga olinmaydi; tugamagan kun o'lchanmaydi.",
            summary, summary.D1Percent, summary.D7Percent, cohorts);
    }
}
