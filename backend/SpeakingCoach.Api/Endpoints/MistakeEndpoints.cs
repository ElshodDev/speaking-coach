using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Endpoints;

public static class MistakeEndpoints
{
    /// <summary>Tuzatishlar olinadigan faoliyatlar: Speaking, Writing, mock (Speaking/Writing) va AI suhbat.</summary>
    private static readonly ActivityType[] Sources = [ActivityType.Speaking, ActivityType.Writing, ActivityType.MockExam, ActivityType.Conversation];

    /// <summary>Ko'pi bilan shuncha oxirgi (tuzatishi bor) faoliyat tahlil qilinadi.</summary>
    public const int MaxActivities = MistakeQuery.MaxActivities;

    /// <summary>So'rov Application'da (AI ustoz tavsiyasi ham foydalanadi); bu yerda — avvalgi nom.</summary>
    public const string CorrectionsSql = MistakeQuery.CorrectionsSql;

    public static void MapMistakeEndpoints(this IEndpointRouteBuilder app)
    {
        // "Mening xatolarim": eng ko'p takrorlanadigan xato turlari (AI'siz hisoblanadi, limitga kirmaydi).
        app.MapGet("/api/mistakes", async (HttpRequest request, AuthService auth, IMistakeSource mistakes) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Json(request.Error("auth.not_logged_in"), statusCode: StatusCodes.Status401Unauthorized);
            var facts = await mistakes.RecentAsync(userId.Value);
            return Results.Ok(MistakeAnalyzer.Summarize(facts, DateTime.UtcNow));
        });
    }

    /// <summary>SQL'dagi turlar ro'yxati Sources bilan bir xil bo'lishi kerak (testda tekshiriladi).</summary>
    public static IReadOnlyList<ActivityType> SourceTypes => Sources;
}
