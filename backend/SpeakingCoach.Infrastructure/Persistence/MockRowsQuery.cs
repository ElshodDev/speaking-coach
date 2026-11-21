using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Infrastructure.Persistence;

/// <summary>Mock urinishlari: kerakli maydonlar Postgres'ning o'zida jsonb'dan o'qiladi (katta matnlar tortilmaydi).</summary>
public class MockRowsQuery(AppDbContext db) : IMockRows
{
    public const string MockRowsSql = """
        SELECT "Id", "CreatedAtUtc",
               "PromptData"->>'exam' AS "Exam",
               "PromptData"->>'module' AS "Module",
               "PromptData"->>'setId' AS "SetId",
               "PromptData"->>'variant' AS "Variant",
               "PromptData"->>'sessionId' AS "SessionId",
               CASE WHEN jsonb_typeof("ResponseData"->'overall') = 'number' THEN ("ResponseData"->>'overall')::numeric END AS "Overall"
        FROM "Activities"
        WHERE "UserId" = {0} AND "Type" = 'MockExam'
        ORDER BY "CreatedAtUtc" DESC
        LIMIT {1}
        """;


    public Task<List<MockQueries.MockRow>> ForUserAsync(Guid userId, int take, CancellationToken ct = default) =>
        db.Database.SqlQueryRaw<MockQueries.MockRow>(MockRowsSql, userId, take).ToListAsync(ct);
}
