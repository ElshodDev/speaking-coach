using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Endpoints;

public record RatingRequest(string? Key, string? Page);

/// <summary>
/// Bir bosishli baholar (👍/👎, "AI bahosi adolatlimi?", "qayerdan topdingiz?"). Ular mavjud Feedbacks jadvaliga
/// Kind = "rating" bilan yoziladi (sxema o'zgarmaydi), kalit esa qat'iy ro'yxatdan (allow-list): erkin matn yo'q,
/// shuning uchun admin xulosasi oddiy guruhlash bilan hisoblanadi. Matnli izoh — mavjud /api/feedback orqali.
/// </summary>
public static partial class RatingRules
{
    public const string Kind = "rating";
    public const int MaxKey = 64;

    [GeneratedRegex(@"^(first_practice:(up|down)|ai_fair:(speaking|writing|part2|mock):(fair|high|low)|source:(instagram|telegram|friend|teacher|search|other))\z", RegexOptions.CultureInvariant)]
    private static partial Regex Allowed();

    public static bool IsValid(string? key) => key is not null && key.Length <= MaxKey && Allowed().IsMatch(key);

    /// <summary>
    /// Bir foydalanuvchi bir marta bera oladigan baholar uchun prefiks ("first_practice:", "source:"); aks holda null
    /// (masalan, AI bahosini har natija uchun alohida berish mumkin).
    /// </summary>
    public static string? OncePrefix(string key) =>
        key.StartsWith("first_practice:", StringComparison.Ordinal) ? "first_practice:"
        : key.StartsWith("source:", StringComparison.Ordinal) ? "source:"
        : null;
}

public static class RatingEndpoints
{
    public static void MapRatingEndpoints(this IEndpointRouteBuilder app)
    {
        // Mehmon ham baholay oladi. Adminga Telegram xabari YUBORILMAYDI (oddiy bosishlar xabarlar oqimini to'ldirmasin).
        app.MapPost("/api/rating", async (RatingRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var key = (body.Key ?? "").Trim();
            if (!RatingRules.IsValid(key)) return Results.BadRequest(request.Error("feedback.bad_kind"));

            var user = await auth.GetCurrentUserAsync(request);
            var once = RatingRules.OncePrefix(key);
            if (once is not null && user is not null)
            {
                var already = await db.Feedbacks.AnyAsync(f => f.UserId == user.Id && f.Kind == RatingRules.Kind && f.Message.StartsWith(once));
                if (already) return Results.Ok(new { ok = true, duplicate = true });
            }

            db.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                UserId = user?.Id,
                Kind = RatingRules.Kind,
                Message = key,
                Page = FeedbackRules.NormalizePage(body.Page),
                Lang = RequestTexts.LangOf(request),
                CreatedAtUtc = DateTime.UtcNow,
                Ip = TelegramLoginRules.Clip(request.HttpContext.Connection.RemoteIpAddress?.ToString(), 64),
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { ok = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // Admin: baholar xulosasi (kalit → soni), oxirgi N kun. 401 — kirmagan; 403 — admin emas.
        app.MapGet("/api/admin/ratings", async (HttpRequest request, AuthService auth, AdminOptions admins, AppDbContext db, int days = 30) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);
            if (!admins.IsAdmin(user)) return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);

            days = Math.Clamp(days, 1, 365);
            var since = DateTime.UtcNow.AddDays(-days);
            var rows = await db.Feedbacks
                .Where(f => f.Kind == RatingRules.Kind && f.CreatedAtUtc >= since)
                .GroupBy(f => f.Message)
                .Select(g => new { key = g.Key, count = g.Count() })
                .ToListAsync();
            return Results.Ok(new { days, items = rows.OrderBy(r => r.key, StringComparer.Ordinal) });
        });
    }
}
