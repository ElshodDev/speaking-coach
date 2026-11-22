namespace SpeakingCoach.Api.Endpoints;

public record FeedbackRequest(string? Kind, string? Message, string? Page);

/// <summary>Fikr-mulohaza: faqat HTTP; qoidalar va saqlash — Application'dagi <see cref="FeedbackService"/>'da.</summary>
public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        static string? ClientIp(HttpRequest r) => r.HttpContext.Connection.RemoteIpAddress?.ToString();

        // Mehmon ham yubora oladi (Telegram/Instagram ichidagi brauzerda kira olmagan odam ham).
        app.MapPost("/api/feedback", async (FeedbackRequest body, HttpRequest request, AuthService auth, FeedbackService feedback) =>
        {
            var error = FeedbackRules.Validate(body.Kind, body.Message);
            if (error is not null)
            {
                return Results.BadRequest(request.ErrorWithCode(error, FeedbackRules.MinMessage, FeedbackRules.MaxMessage));
            }

            var user = await auth.GetCurrentUserAsync(request);
            var saved = await feedback.SaveFeedbackAsync(user, body.Kind!, body.Message!, body.Page, RequestTexts.LangOf(request), ClientIp(request));
            await feedback.NotifyAdminsAsync(saved, user);
            return Results.Ok(new { ok = true, id = saved.Id });
        }).RequireRateLimiting(RateLimits.FeedbackPolicy);

        // Admin: oxirgi fikrlar (eng yangisi birinchi). 401 — kirmagan; 403 — admin emas.
        app.MapGet("/api/admin/feedback", async (HttpRequest request, AuthService auth, AdminOptions admins, FeedbackService feedback, int take = 50) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);
            if (!admins.IsAdmin(user)) return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);
            return Results.Ok(await feedback.RecentFeedbackAsync(take));
        });
    }
}
