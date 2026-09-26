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

    private static IResult Unauthorized(HttpRequest request) =>
        Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);

    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        // Bugungi AI limiti: qancha ishlatildi va qancha qoldi (mehmon uchun ham).
        app.MapGet("/api/usage", async (HttpRequest request, AiQuotaService quotas) =>
        {
            var s = await quotas.GetStatusAsync(request);
            return Results.Ok(new
            {
                exercises = new { used = s.Exercises.Used, limit = s.Exercises.Limit, left = s.Exercises.Left },
                words = new { used = s.Words.Used, limit = s.Words.Limit, left = s.Words.Left },
                unlimited = s.Unlimited,
                guest = s.Guest,
            });
        });

        app.MapGet("/api/profile", async (HttpRequest request, AuthService auth, AdminOptions admins) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
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
            if (user is null) return Unauthorized(request);

            if (body.Level is not null && !LearnerLevel.IsValid(body.Level))
            {
                return Results.BadRequest(request.Error("profile.bad_level"));
            }

            var name = string.IsNullOrWhiteSpace(body.DisplayName) ? null : body.DisplayName.Trim();
            if (name is not null && !DisplayNamePattern().IsMatch(name))
            {
                return Results.BadRequest(request.Error("profile.bad_nickname"));
            }
            if (body.ShowOnLeaderboard && name is null)
            {
                return Results.BadRequest(request.Error("profile.nickname_required"));
            }
            if (name is not null)
            {
                var lower = name.ToLower();
                var taken = await db.Users.AnyAsync(u => u.Id != user.Id && u.DisplayName != null && u.DisplayName.ToLower() == lower);
                if (taken) return Results.BadRequest(request.Error("profile.nickname_taken"));
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
            if (userId is null) return Unauthorized(request);
            return Results.Ok(await progress.GetProgressAsync(userId.Value, Math.Clamp(tzOffsetMinutes, -840, 840)));
        });

        app.MapGet("/api/leaderboard", async (HttpRequest request, AuthService auth, ProgressService progress) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized(request);
            return Results.Ok(await progress.GetLeaderboardAsync(userId.Value));
        });

        // Admin: 401 — kirmagan; 403 — kirgan, lekin admin emas.
        app.MapGet("/api/admin/overview", async (HttpRequest request, AuthService auth, AdminOptions admins, AdminService admin, int tzOffsetMinutes = 0) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (!admins.IsAdmin(user))
            {
                return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);
            }
            return Results.Ok(await admin.GetOverviewAsync(Math.Clamp(tzOffsetMinutes, -840, 840)));
        });

        // Tizim holati: baza sxemasi, Telegram bot, sozlamalar. Sirlar qaytarilmaydi.
        app.MapGet("/api/admin/system", async (
            HttpRequest request, AuthService auth, AdminOptions admins, AppDbContext db,
            SpeakingCoach.Api.Services.Telegram.ITelegramApi telegram, SpeakingCoach.Api.Services.Telegram.TelegramOptions tg,
            IEmailSender email, IGoogleSignIn google, IConfiguration config) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (!admins.IsAdmin(user))
            {
                return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);
            }
            return Results.Ok(await SystemHealth.GetAsync(db, telegram, tg, email, google, config, request.HttpContext.RequestAborted));
        });

        // So'z ma'nosi — mehmonga ham ochiq (faqat kartaga saqlash uchun kirish kerak).
        // Tarjima so'rov tilida: o'zbekcha, ruscha yoki (en) sodda inglizcha sinonim.
        app.MapPost("/api/words/explain", async (WordExplainRequest body, HttpRequest request, IWordService words, AiQuotaService quotas, ILogger<Program> logger) =>
        {
            var clean = GeminiWordService.Sanitize(body.Word, body.Sentence, out var errorKey);
            if (clean is null) return Results.BadRequest(request.Error(errorKey!));
            var limited = await quotas.CheckAsync(request, AiKind.Word);
            if (limited is not null) return limited;
            try
            {
                var explanation = await words.ExplainAsync(
                    clean.Value.Word, clean.Value.Sentence, LearnerLevel.Normalize(body.Level), Texts.LangOf(request));
                await quotas.RecordAsync(request, AiKind.Word);
                return Results.Ok(explanation);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "So'z izohida xato: {Word}", clean.Value.Word);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting("ai");
    }
}
