using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

/// <summary>Guruh vazifalari bo'yicha o'quvchilar holati (o'qituvchi jadvali, o'quvchining vazifalari, bosh sahifa).</summary>
public static class GroupQueries
{
    /// <summary>Har bir vazifa × o'quvchi uchun holat (o'quvchilarning urinishlaridan avtomatik).</summary>
    public static async Task<Dictionary<Guid, Dictionary<Guid, AssignmentStatus>>> ResultsAsync(IAppDbContext db, List<Assignment> assignments, List<Guid> userIds)
    {
        var map = assignments.ToDictionary(a => a.Id, _ => new Dictionary<Guid, AssignmentStatus>());
        if (assignments.Count == 0 || userIds.Count == 0)
        {
            foreach (var a in assignments)
                foreach (var u in userIds) map[a.Id][u] = new(false, false, null, null, null, null, null);
            return map;
        }

        var since = assignments.Min(a => a.CreatedAtUtc);
        // Faqat vazifalarga mos turlar va faqat kerakli ustunlar: ResponseData (baholar,
        // insho tahlili) bu yerda o'qilmaydi — ball pastda faqat topilgan urinishlar uchun olinadi.
        var types = GroupLogic.NeededTypes(assignments).ToList();
        var rows = types.Count == 0 ? [] : await db.Activities
            .Where(x => x.UserId != null && userIds.Contains(x.UserId.Value) && x.CreatedAtUtc >= since && types.Contains(x.Type))
            .Select(x => new { x.Id, UserId = x.UserId!.Value, x.Type, x.CreatedAtUtc, PromptData = x.Type == ActivityType.MockExam ? x.PromptData : null })
            .ToListAsync();
        var byUser = rows.Select(x =>
        {
            string? exam = null, module = null;
            if (x.Type == ActivityType.MockExam)
            {
                var p = MockQueries.ParsePrompt(x.PromptData ?? "");
                (exam, module) = (p.Exam, p.Module);
            }
            return (x.UserId, Ref: new ActivityRef(x.Id, x.Type, x.CreatedAtUtc, exam, module, null));
        }).ToLookup(x => x.UserId, x => x.Ref);

        var reviewsByUser = Enumerable.Empty<(Guid UserId, DateTime At)>().ToLookup(r => r.UserId, r => r.At);
        if (assignments.Any(a => a.Kind == "review"))
        {
            var logs = await db.ReviewLogs.Where(l => userIds.Contains(l.UserId) && l.ReviewedAtUtc >= since).Select(l => new { l.UserId, l.ReviewedAtUtc }).ToListAsync();
            reviewsByUser = logs.ToLookup(l => l.UserId, l => l.ReviewedAtUtc);
        }

        foreach (var u in userIds)
        {
            var mine = byUser[u].ToList();
            var myReviews = reviewsByUser[u].ToList();
            foreach (var a in assignments) map[a.Id][u] = GroupLogic.StatusFor(a, mine, myReviews);
        }

        // Ball — faqat jadvalda ko'rsatiladigan (vazifaga hisoblangan) urinishlar uchun.
        var hitIds = map.Values.SelectMany(m => m.Values).Where(st => st.ActivityId is not null).Select(st => st.ActivityId!.Value).Distinct().ToList();
        if (hitIds.Count > 0)
        {
            var scores = new Dictionary<Guid, string?>();
            foreach (var chunk in hitIds.Chunk(500))
            {
                var part = await db.Activities.Where(x => chunk.Contains(x.Id)).Select(x => new { x.Id, x.ResponseData }).ToListAsync();
                foreach (var r in part) scores[r.Id] = GroupLogic.ScoreText(r.ResponseData);
            }
            foreach (var m in map.Values)
                foreach (var u in m.Keys.ToList())
                    if (m[u].ActivityId is Guid aid && scores.TryGetValue(aid, out var sc)) m[u] = m[u] with { Score = sc };
        }
        return map;
    }
}
