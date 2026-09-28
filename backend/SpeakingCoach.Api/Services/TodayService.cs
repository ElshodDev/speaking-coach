using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Endpoints;

namespace SpeakingCoach.Api.Services;

/// <summary>O'quvchining bajarilmagan o'qituvchi vazifasi (sayt, bot va eslatma uchun umumiy).</summary>
public record PendingTask(Guid Id, string GroupName, string Kind, int? Target, string Instructions, DateTime? DueAtUtc, AssignmentStatus Status);

/// <summary>
/// "Bugun" ma'lumotlari: kunlik reja va bajarilmagan vazifalar. Sayt
/// (/api/plan/today), Telegram bot (📋 Bugun) va kechki eslatma bir xil
/// hisobni ishlatadi — natijalar hamma joyda bir xil.
/// </summary>
public class TodayService(AppDbContext db, ReviewService reviews)
{
    public async Task<(PlanLogic.Plan Plan, ReviewStats Stats)> PlanAsync(User user, int tzOffsetMinutes)
    {
        var offset = Math.Clamp(tzOffsetMinutes, -840, 840);
        var now = DateTime.UtcNow;
        var today = ReviewScheduler.ToLocalDate(now, offset);
        var stats = await reviews.GetStatsAsync(user.Id, offset);

        var since = now.AddDays(-60);
        // PromptData faqat mock uchun kerak (qaysi imtihon/bo'lim) — o'qish matnlari va
        // insholar bazadan tortilmaydi (CASE SQL'ning o'zida).
        var rows = await db.Activities
            .Where(a => a.UserId == user.Id && a.CreatedAtUtc >= since)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(1000)
            .Select(a => new { a.Type, a.CreatedAtUtc, PromptData = a.Type == ActivityType.MockExam ? a.PromptData : "{}" })
            .ToListAsync();
        var practiced = rows
            .Where(r => ReviewScheduler.ToLocalDate(r.CreatedAtUtc, offset) == today)
            .Select(r => PlanLogic.SkillOf(r.Type)).OfType<string>().ToHashSet();
        var exam = user.Goal is "ielts" or "cefr" ? user.Goal : null;
        var mocks = rows
            .Where(r => r.Type == ActivityType.MockExam)
            .Select(r => (r.CreatedAtUtc, P: MockEndpoints.ParsePrompt(r.PromptData)))
            .Where(x => x.P.Exam == exam && PlanLogic.Skills.Contains(x.P.Module))
            .ToList();
        var mockedToday = mocks.Where(x => ReviewScheduler.ToLocalDate(x.CreatedAtUtc, offset) == today).Select(x => x.P.Module).ToHashSet();
        var lastMock = mocks.GroupBy(x => x.P.Module).ToDictionary(g => g.Key, g => g.Max(x => x.CreatedAtUtc));

        var plan = PlanLogic.Build(new PlanLogic.Input(
            user.Goal, user.DailyMinutes, user.ExamDate, today,
            stats.Total, stats.Due, stats.ReviewedToday, stats.DailyGoal,
            practiced, mockedToday, lastMock));
        return (plan, stats);
    }

    /// <summary>Bajarilmagan vazifalar: avval muddati yaqinlari (muddatsizlar oxirida).</summary>
    public async Task<List<PendingTask>> PendingTasksAsync(Guid userId)
    {
        var groupIds = await db.GroupMembers.Where(m => m.UserId == userId).Select(m => m.GroupId).ToListAsync();
        if (groupIds.Count == 0) return [];
        var groups = await db.Groups.Where(g => groupIds.Contains(g.Id)).Select(g => new { g.Id, g.Name }).ToListAsync();
        var assignments = await db.Assignments.Where(a => groupIds.Contains(a.GroupId)).OrderByDescending(a => a.CreatedAtUtc).Take(200).ToListAsync();
        var results = await GroupEndpoints.ResultsAsync(db, assignments, [userId]);
        return assignments
            .Select(a => new PendingTask(a.Id, groups.First(g => g.Id == a.GroupId).Name, a.Kind, a.Target, a.Instructions, a.DueAtUtc, results[a.Id][userId]))
            .Where(t => !t.Status.Done)
            .OrderBy(t => t.DueAtUtc ?? DateTime.MaxValue)
            .ToList();
    }
}
