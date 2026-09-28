using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public static class MistakeEndpoints
{
    /// <summary>Tuzatishlar olinadigan faoliyatlar: Speaking, Writing, mock (Speaking/Writing) va AI suhbat.</summary>
    private static readonly ActivityType[] Sources = [ActivityType.Speaking, ActivityType.Writing, ActivityType.MockExam, ActivityType.Conversation];

    /// <summary>Ko'pi bilan shuncha oxirgi (tuzatishi bor) faoliyat tahlil qilinadi.</summary>
    public const int MaxActivities = 400;

    /// <summary>Faqat "topCorrections" — natijaning qolgan qismi (ballar, izohlar, Listening/Reading javoblari) bazadan tortilmaydi.</summary>
    public sealed class CorrectionsRow
    {
        public DateTime CreatedAtUtc { get; set; }
        public string Json { get; set; } = "{}";
    }

    public const string CorrectionsSql = """
        SELECT "CreatedAtUtc", jsonb_build_object('topCorrections', "ResponseData"->'topCorrections')::text AS "Json"
        FROM "Activities"
        WHERE "UserId" = {0} AND "Type" IN ('Speaking', 'Writing', 'MockExam', 'Conversation')
          AND jsonb_typeof("ResponseData"->'topCorrections') = 'array'
        ORDER BY "CreatedAtUtc" DESC
        LIMIT {1}
        """;

    public static void MapMistakeEndpoints(this IEndpointRouteBuilder app)
    {
        // "Mening xatolarim": eng ko'p takrorlanadigan xato turlari (AI'siz hisoblanadi, limitga kirmaydi).
        app.MapGet("/api/mistakes", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Json(request.Error("auth.not_logged_in"), statusCode: StatusCodes.Status401Unauthorized);
            var rows = await db.Database.SqlQueryRaw<CorrectionsRow>(CorrectionsSql, userId.Value, MaxActivities).ToListAsync();
            var facts = rows.SelectMany(r => MistakeAnalyzer.FromResponse(r.Json, r.CreatedAtUtc));
            return Results.Ok(MistakeAnalyzer.Summarize(facts, DateTime.UtcNow));
        });
    }

    /// <summary>SQL'dagi turlar ro'yxati Sources bilan bir xil bo'lishi kerak (testda tekshiriladi).</summary>
    public static IReadOnlyList<ActivityType> SourceTypes => Sources;
}
