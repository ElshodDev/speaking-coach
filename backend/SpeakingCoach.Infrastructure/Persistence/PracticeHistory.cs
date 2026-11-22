using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Infrastructure.Persistence;

/// <summary>Postgres'ga xos: sarlavha jsonb'dan o'qiladi (butun PromptData tortilmaydi).</summary>
public class PracticeHistory(AppDbContext db) : IPracticeHistory
{
    public const string DoneTitlesSql = """
        SELECT "PromptData"->>'title' AS "Value"
        FROM "Activities"
        WHERE "UserId" = {0} AND "Type" = {1} AND jsonb_typeof("PromptData"->'title') = 'string'
        ORDER BY "CreatedAtUtc" DESC
        LIMIT 300
        """;

    public Task<List<string>> DoneTitlesAsync(Guid userId, ActivityType type) =>
        db.Database.SqlQueryRaw<string>(DoneTitlesSql, userId, type.ToString()).ToListAsync();

    public Task<List<string>> Part2PromptsAsync(Guid userId) =>
        db.Activities
            .Where(a => a.UserId == userId && a.Type == ActivityType.Speaking && EF.Functions.JsonExists(a.PromptData, "part2"))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(300)
            .Select(a => a.PromptData)
            .ToListAsync();

    public Task<List<FluencyRow>> FluencyRowsAsync(Guid userId, int take)
    {
        ActivityType[] types = [ActivityType.Speaking, ActivityType.MockExam, ActivityType.Conversation];
        return db.Activities
            .Where(a => a.UserId == userId && types.Contains(a.Type) && EF.Functions.JsonExists(a.ResponseData, "metrics"))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(take)
            .Select(a => new FluencyRow(a.CreatedAtUtc, a.Type, a.ResponseData))
            .ToListAsync();
    }
}
