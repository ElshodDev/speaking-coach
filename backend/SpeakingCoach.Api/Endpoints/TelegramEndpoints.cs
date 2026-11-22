using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                // Telegram ulanishni uzsa ham (u javobni ~60 s kutadi) boshlangan
                // ish yarim yo'lda qolmasin — o'z vaqt chegarasi bilan tugatiladi.
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                await bot.HandleAsync(update, cts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telegram yangilanishida kutilmagan xato: {UpdateId}", update.UpdateId);
            }
            return Results.Ok();
        });

        // GitHub Actions har soatda chaqiradi (.github/workflows/telegram-reminders.yml).
        // Ish fonda bajariladi (ReminderJob) — javob darhol 202, natija logda.
        app.MapPost("/api/telegram/cron", (HttpRequest request, TelegramOptions options, ReminderJob job) =>
        {
            if (!options.Enabled || options.CronSecret is null) return Results.NotFound();
            if (!SecretEquals(request.Headers["X-Cron-Secret"].ToString(), options.CronSecret)) return Results.Unauthorized();
            var started = job.TryStart(DateTime.UtcNow);
            return Results.Accepted(value: new { started, alreadyRunning = !started });
        });

        // ---- Sayt uchun (Profil → Telegram) ----

        static IResult Unauthorized(HttpRequest request) =>
            Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);

        app.MapGet("/api/telegram/status", async (HttpRequest request, AuthService auth, TelegramLinkService links) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Results.Ok(await links.StatusAsync(user.Id));
        });

        // Bir martalik havola: t.me/Bot?start=TOKEN (15 daqiqa amal qiladi).
        app.MapPost("/api/telegram/link", async (TelegramLinkRequest body, HttpRequest request, AuthService auth, TelegramLinkService links) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var r = await links.CreateLinkAsync(user, body.TzOffsetMinutes, body.Lang, RequestTexts.LangOf(request));
            if (r.Disabled) return Results.NotFound();
            if (r.DemoBlocked) return Results.Json(request.Error("demo.not_allowed"), statusCode: StatusCodes.Status403Forbidden);
            return Results.Ok(new { url = r.Url });
        }).RequireRateLimiting("auth");

        app.MapPut("/api/telegram/settings", async (TelegramSettingsRequest body, HttpRequest request, AuthService auth, TelegramLinkService links) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var (found, hour) = await links.SetReminderHourAsync(user.Id, body.ReminderHour);
            return found ? Results.Ok(new { reminderHour = hour }) : Results.NotFound();
        });

        app.MapDelete("/api/telegram/link", async (HttpRequest request, AuthService auth, TelegramLinkService links) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            await links.UnlinkAsync(user.Id);
            return Results.Ok();
        });
    }
}
