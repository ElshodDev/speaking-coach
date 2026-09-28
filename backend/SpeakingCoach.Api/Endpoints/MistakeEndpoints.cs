using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public static class MistakeEndpoints
{
    /// <summary>Tuzatishlar olinadigan faoliyatlar: Speaking, Writing, mock (Speaking/Writing) va AI suhbat.</summary>
    private static readonly ActivityType[] Sources = [ActivityType.Speaking, ActivityType.Writing, ActivityType.MockExam, ActivityType.Conversation];

    public static void MapMistakeEndpoints(this IEndpointRouteBuilder app)
    {
        // "Mening xatolarim": eng ko'p takrorlanadigan xato turlari (AI'siz hisoblanadi, limitga kirmaydi).
        app.MapGet("/api/mistakes", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Json(request.Error("auth.not_logged_in"), statusCode: StatusCodes.Status401Unauthorized);
            var rows = await db.Activities
                .Where(a => a.UserId == userId && Sources.Contains(a.Type))
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(400)
                .Select(a => new { a.CreatedAtUtc, a.ResponseData })
                .ToListAsync();
            var facts = rows.SelectMany(r => MistakeAnalyzer.FromResponse(r.ResponseData, r.CreatedAtUtc));
            return Results.Ok(MistakeAnalyzer.Summarize(facts, DateTime.UtcNow));
        });
    }
}
