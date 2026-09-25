using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record ProfileUpdateRequest(string? DisplayName, string? Level, bool ShowOnLeaderboard);
public record WordExplainRequest(string Word, string? Sentence, string? Level);

public static partial class ProfileEndpoints
{
    // Taxallus: harflar (o'zbekcha ham), raqamlar, bo'sh joy va . _ - ' belgilari.
    [GeneratedRegex(@"^[\p{L}\p{N} ._'\-]{2,30}$")]
    private static partial Regex DisplayNamePattern();

    private static IResult Unauthorized() =>
        Results.Json(new { error = "Tizimga kiring" }, statusCode: StatusCodes.Status401Unauthorized);

    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profile", async (HttpRequest request, AuthService auth, AdminOptions admins) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized();
            return Results.Ok(new
            {
                email = user.Email,
                displayName = user.DisplayName,
                level = LearnerLevel.Normalize(user.Level),
                showOnLeaderboard = user.ShowOnLeaderboard,
                isAdmin = admins.IsAdmin(user),
            });
        });

        app.MapPut("/api/profile", async (ProfileUpdateRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized();

            if (body.Level is not null && !LearnerLevel.IsValid(body.Level))
            {
                return Results.BadRequest(new { error = "Daraja A2, B1, B2 yoki C1 bo'lishi kerak" });
            }

            var name = string.IsNullOrWhiteSpace(body.DisplayName) ? null : body.DisplayName.Trim();
            if (name is not null && !DisplayNamePattern().IsMatch(name))
            {
                return Results.BadRequest(new { error = "Taxallus 2-30 belgi: harf, raqam, bo'sh joy va . _ - ' belgilari" });
            }
            if (body.ShowOnLeaderboard && name is null)
            {
                return Results.BadRequest(new { error = "Musobaqada qatnashish uchun taxallus kiriting" });
            }
            if (name is not null)
            {
                var lower = name.ToLower();
                var taken = await db.Users.AnyAsync(u => u.Id != user.Id && u.DisplayName != null && u.DisplayName.ToLower() == lower);
                if (taken) return Results.BadRequest(new { error = "Bu taxallus band — boshqasini tanlang" });
            }

            user.DisplayName = name;
            if (body.Level is not null) user.Level = LearnerLevel.Normalize(body.Level);
            user.ShowOnLeaderboard = body.ShowOnLeaderboard;
            await db.SaveChangesAsync();
            return Results.Ok(new { displayName = user.DisplayName, level = user.Level, showOnLeaderboard = user.ShowOnLeaderboard });
        });

        app.MapGet("/api/progress", async (HttpRequest request, AuthService auth, ProgressService progress, int tzOffsetMinutes = 0) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();
            return Results.Ok(await progress.GetProgressAsync(userId.Value, Math.Clamp(tzOffsetMinutes, -840, 840)));
        });

        app.MapGet("/api/leaderboard", async (HttpRequest request, AuthService auth, ProgressService progress) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();
            return Results.Ok(await progress.GetLeaderboardAsync(userId.Value));
        });

        // Admin: 401 — kirmagan; 403 — kirgan, lekin admin emas.
        app.MapGet("/api/admin/overview", async (HttpRequest request, AuthService auth, AdminOptions admins, AdminService admin, int tzOffsetMinutes = 0) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized();
            if (!admins.IsAdmin(user))
            {
                return Results.Json(new { error = "Bu sahifa faqat admin uchun" }, statusCode: StatusCodes.Status403Forbidden);
            }
            return Results.Ok(await admin.GetOverviewAsync(Math.Clamp(tzOffsetMinutes, -840, 840)));
        });

        // So'z ma'nosi — mehmonga ham ochiq (faqat kartaga saqlash uchun kirish kerak).
        app.MapPost("/api/words/explain", async (WordExplainRequest body, IWordService words, ILogger<Program> logger) =>
        {
            var clean = GeminiWordService.Sanitize(body.Word, body.Sentence, out var error);
            if (clean is null) return Results.BadRequest(new { error });
            try
            {
                return Results.Ok(await words.ExplainAsync(clean.Value.Word, clean.Value.Sentence, LearnerLevel.Normalize(body.Level)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "So'z izohida xato: {Word}", clean.Value.Word);
                return Results.Problem(detail: ex.Message, statusCode: 502);
            }
        }).RequireRateLimiting("ai");
    }
}
