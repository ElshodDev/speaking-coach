namespace SpeakingCoach.Api.Endpoints;

public record RatingRequest(string? Key, string? Page);

/// <summary>
/// Bir bosishli baholar (👍/👎, "AI bahosi adolatlimi?", "qayerdan topdingiz?") — faqat HTTP; qoidalar (allow-list, bir martalik turlar)
/// va saqlash Application'dagi <see cref="FeedbackService"/>'da. Mavjud Feedbacks jadvaliga Kind = "rating" bilan yoziladi.
/// </summary>
public static class RatingEndpoints
{
    public static void MapRatingEndpoints(this IEndpointRouteBuilder app)
    {
        // Mehmon ham baholay oladi. Adminga Telegram xabari YUBORILMAYDI (oddiy bosishlar xabarlar oqimini to'ldirmasin).
        app.MapPost("/api/rating", async (RatingRequest body, HttpRequest request, AuthService auth, FeedbackService feedback) =>
        {
            var key = (body.Key ?? "").Trim();
            if (!RatingRules.IsValid(key)) return Results.BadRequest(request.Error("feedback.bad_kind"));

            var user = await auth.GetCurrentUserAsync(request);
            var duplicate = await feedback.SaveRatingAsync(user?.Id, key, body.Page, RequestTexts.LangOf(request), request.HttpContext.Connection.RemoteIpAddress?.ToString());
            return duplicate ? Results.Ok(new { ok = true, duplicate = true }) : Results.Ok(new { ok = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // Admin: baholar xulosasi (kalit → soni), oxirgi N kun. 401 — kirmagan; 403 — admin emas.
        app.MapGet("/api/admin/ratings", async (HttpRequest request, AuthService auth, AdminOptions admins, FeedbackService feedback, int days = 30) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);
            if (!admins.IsAdmin(user)) return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);

            var (d, items) = await feedback.RatingSummaryAsync(days);
            return Results.Ok(new { days = d, items = items.Select(i => new { key = i.Key, count = i.Count }) });
        });
    }
}
