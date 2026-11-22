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
}
