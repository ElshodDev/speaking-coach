using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Endpoints;

public record TelegramLinkRequest(int TzOffsetMinutes, string? Lang);
public record TelegramSettingsRequest(int? ReminderHour);

public static class TelegramEndpoints
{
    private static bool SecretEquals(string? given, string? expected) =>
        given is not null && expected is not null &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));

    public static void MapTelegramEndpoints(this IEndpointRouteBuilder app)
    {
        // Telegram → bizning server. So'rov haqiqatan Telegram'dan kelganini
        // maxfiy sarlavha bilan tekshiramiz. Doim 200 qaytaramiz: aks holda
        // Telegram bir xil yangilanishni qayta-qayta yuboraveradi.
        app.MapPost("/api/telegram/webhook", async (HttpRequest request, TelegramOptions options, TelegramBot bot, ILogger<Program> logger) =>
        {
            if (!options.Enabled) return Results.NotFound();
            if (!SecretEquals(request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString(), options.WebhookSecret))
            {
                return Results.Unauthorized();
            }

            TgUpdate? update;
            try
            {
                update = await JsonSerializer.DeserializeAsync<TgUpdate>(request.Body);
            }
            catch (JsonException)
            {
                return Results.Ok();
            }
            if (update is null) return Results.Ok();

            try
            {
                await bot.HandleAsync(update, request.HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telegram yangilanishida kutilmagan xato: {UpdateId}", update.UpdateId);
            }
            return Results.Ok();
        });

        // GitHub Actions har soatda chaqiradi (.github/workflows/telegram-reminders.yml).
        app.MapPost("/api/telegram/cron", async (HttpRequest request, TelegramOptions options, TelegramReminders reminders) =>
        {
            if (!options.Enabled || options.CronSecret is null) return Results.NotFound();
            if (!SecretEquals(request.Headers["X-Cron-Secret"].ToString(), options.CronSecret)) return Results.Unauthorized();
            return Results.Ok(await reminders.RunAsync(DateTime.UtcNow, request.HttpContext.RequestAborted));
        });

        // ---- Sayt uchun (Profil → Telegram) ----

        static IResult Unauthorized(HttpRequest request) =>
            Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);

        app.MapGet("/api/telegram/status", async (HttpRequest request, AuthService auth, AppDbContext db, TelegramOptions options) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var account = await db.TelegramAccounts.FirstOrDefaultAsync(a => a.UserId == user.Id);
            return Results.Ok(new
            {
                enabled = options.Enabled,
                botUsername = options.BotUsername,
                linked = account is not null,
                username = account?.Username,
                reminderHour = account?.ReminderHour,
            });
        });

        // Bir martalik havola: t.me/Bot?start=TOKEN (15 daqiqa amal qiladi).
        app.MapPost("/api/telegram/link", async (TelegramLinkRequest body, HttpRequest request, AuthService auth, AppDbContext db, TelegramOptions options) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (!options.Enabled) return Results.NotFound();

            var now = DateTime.UtcNow;
            db.TelegramLinkTokens.RemoveRange(await db.TelegramLinkTokens
                .Where(t => t.UserId == user.Id || t.ExpiresAtUtc < now).ToListAsync());
            var token = BotLogic.NewLinkToken();
            db.TelegramLinkTokens.Add(new TelegramLinkToken
            {
                TokenHash = BotLogic.HashToken(token),
                UserId = user.Id,
                Lang = Texts.Langs.Contains(body.Lang) ? body.Lang! : Texts.LangOf(request),
                TzOffsetMinutes = Math.Clamp(body.TzOffsetMinutes, -14 * 60, 14 * 60),
                ExpiresAtUtc = now.Add(BotLogic.LinkTokenLifetime),
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { url = $"https://t.me/{options.BotUsername}?start={token}" });
        }).RequireRateLimiting("auth");

        app.MapPut("/api/telegram/settings", async (TelegramSettingsRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var account = await db.TelegramAccounts.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (account is null) return Results.NotFound();
            account.ReminderHour = body.ReminderHour is int h ? Math.Clamp(h, 0, 23) : null;
            await db.SaveChangesAsync();
            return Results.Ok(new { reminderHour = account.ReminderHour });
        });

        app.MapDelete("/api/telegram/link", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            db.TelegramAccounts.RemoveRange(await db.TelegramAccounts.Where(a => a.UserId == user.Id).ToListAsync());
            await db.SaveChangesAsync();
            return Results.Ok();
        });
    }
}
