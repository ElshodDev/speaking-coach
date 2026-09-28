using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Endpoints;

public record AuthRequest(string Email, string Password);
public record VerifyRequest(string Email, string Code, string? Password = null);
public record EmailRequest(string Email);
public record ResetRequest(string Email, string Code, string NewPassword);
public record GoogleRequest(string Credential);
public record TelegramAuthRequest(string InitData);
public record DemoRequest(int TzOffsetMinutes = -300);
public record TelegramLoginStartRequest(string? Lang = null);
public record TelegramPollRequest(string? PollToken);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Barcha natijalar bir xil shaklda: token (kirildi), needsVerification
        // (emailga kod yuborildi — kodni so'rang), ok (amal bajarildi) yoki
        // so'rov tilidagi xato matni.
        static IResult ToResult(HttpRequest request, AuthResult result)
        {
            if (result.Error is not null)
            {
                return Results.Json(request.ErrorWithCode(result.Error, result.ErrorArgs ?? []), statusCode: result.Status);
            }
            if (result.NeedsVerification)
            {
                return Results.Ok(new { needsVerification = true, email = result.Email });
            }
            return result.Token is not null
                ? Results.Ok(new { token = result.Token, email = result.Email })
                : Results.Ok(new { ok = true });
        }

        app.MapPost("/api/auth/register", async (AuthRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.RegisterAsync(body.Email, body.Password, Texts.LangOf(request))))
            .RequireRateLimiting("auth");

        app.MapPost("/api/auth/login", async (AuthRequest body, HttpRequest request, AuthService auth) =>
            // Noto'g'ri parol — 401 ("kim ekaningizni tasdiqlab bo'lmadi"), 400 emas.
            ToResult(request, await auth.LoginAsync(body.Email, body.Password, Texts.LangOf(request))))
            .RequireRateLimiting("auth");

        app.MapPost("/api/auth/verify", async (VerifyRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.VerifyEmailAsync(body.Email, body.Code, body.Password)))
            .RequireRateLimiting("auth");

        app.MapPost("/api/auth/resend", async (EmailRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.ResendAsync(body.Email, EmailCodePurpose.Verify, Texts.LangOf(request))))
            .RequireRateLimiting("code");

        app.MapPost("/api/auth/forgot", async (EmailRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.ForgotPasswordAsync(body.Email, Texts.LangOf(request))))
            .RequireRateLimiting("code");

        app.MapPost("/api/auth/reset", async (ResetRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.ResetPasswordAsync(body.Email, body.Code, body.NewPassword)))
            .RequireRateLimiting("auth");

        // Parolsiz kirish (telefonda qulay): emailga 6 xonali kod → kod bilan kirish.
        // Hisob bo'lmasa, birinchi kodda ochiladi — alohida "ro'yxatdan o'tish" qadami yo'q.
        app.MapPost("/api/auth/code/request", async (EmailRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.RequestLoginCodeAsync(body.Email, Texts.LangOf(request))))
            .RequireRateLimiting("code");

        app.MapPost("/api/auth/code/verify", async (VerifyRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.LoginWithCodeAsync(body.Email, body.Code)))
            .RequireRateLimiting("auth");

        // Brauzer Google'dan olgan ID token'ni yuboradi; server uni o'zi tekshiradi.
        app.MapPost("/api/auth/google", async (GoogleRequest body, HttpRequest request, IGoogleSignIn google, AuthService auth) =>
        {
            if (google.ClientId is null)
            {
                return Results.Json(request.Error("google.unavailable"), statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            var payload = await google.ValidateAsync(body.Credential);
            return payload is null
                ? Results.Json(request.Error("google.invalid"), statusCode: StatusCodes.Status401Unauthorized)
                : ToResult(request, await auth.GoogleSignInAsync(payload));
        }).RequireRateLimiting("auth");

        // Telegram Mini App: sayt Telegram ichida ochilganda imzolangan initData
        // yuboriladi. Bot bilan ulangan hisob bo'lsa — o'sha hisobga kiriladi;
        // bo'lmasa — Telegram orqali yangi hisob ochiladi (parolsiz).
        app.MapPost("/api/auth/telegram", async (TelegramAuthRequest body, HttpRequest request, TelegramOptions options, AuthService auth) =>
        {
            var tg = TelegramWebAppAuth.ValidateUser(body.InitData, options.Token, DateTime.UtcNow);
            if (tg is null) return Results.Json(request.ErrorWithCode("auth.telegram_invalid"), statusCode: StatusCodes.Status401Unauthorized);
            var (result, isNew) = await auth.SignInTelegramAsync(tg, SignupMethods.TelegramApp, Texts.LangOf(request));
            return Results.Ok(new { token = result.Token, email = result.Email, isNew });
        }).RequireRateLimiting("auth");

        // Saytdan Telegram orqali kirish (Telegram/Instagram ichidagi brauzerda
        // Google ishlamaydi, email esa o'chiq yoki limiti tugagan bo'lishi mumkin):
        // 1) start → bot havolasi + 2 xonali kod; 2) botda shu raqam bosiladi;
        // 3) sayt poll qilib sessiyani oladi.
        app.MapPost("/api/auth/telegram/start", async (TelegramLoginStartRequest? body, HttpRequest request, TelegramOptions options, AuthService auth) =>
        {
            if (!options.Enabled)
            {
                return Results.Json(request.ErrorWithCode("tglogin.disabled"), statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            var lang = Texts.Langs.Contains(body?.Lang) ? body!.Lang! : Texts.LangOf(request);
            var start = await auth.StartTelegramLoginAsync(lang, request.HttpContext.Connection.RemoteIpAddress?.ToString());
            return Results.Ok(new
            {
                loginId = start.LoginId.ToString("N"),
                pollToken = start.PollToken,
                botUrl = $"https://t.me/{options.BotUsername}?start={TelegramLoginRules.StartPrefix}{start.Nonce}",
                code = start.Code,
                expiresInSeconds = start.ExpiresInSeconds,
            });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/telegram/poll", async (TelegramPollRequest body, AuthService auth) =>
        {
            var r = await auth.PollTelegramLoginAsync(body.PollToken);
            return r.Token is null
                ? Results.Ok(new { status = r.Status })
                : Results.Ok(new { status = r.Status, token = r.Token, email = r.Email, isNew = r.IsNew });
        }).RequireRateLimiting("poll");

        // Frontend "Parolni unutdim" va "Google bilan kirish" tugmalarini faqat
        // tegishli xizmat sozlangan bo'lsa ko'rsatadi. Client ID maxfiy emas.
        app.MapGet("/api/auth/config", (AuthService auth, IGoogleSignIn google, IConfiguration config, TelegramOptions telegram) =>
            Results.Ok(new
            {
                emailVerification = auth.VerificationRequired,
                googleClientId = google.ClientId,
                demo = DemoAccount.Enabled(config),
                telegramLogin = telegram.Enabled,
                botUsername = telegram.Enabled ? telegram.BotUsername : null,
            }));

        // Demo: ro'yxatdan o'tmasdan, tayyor ma'lumotli vaqtinchalik hisob (24 soat).
        app.MapPost("/api/auth/demo", async (DemoRequest? body, HttpRequest request, AuthService auth, DemoGate gate, IConfiguration config) =>
        {
            if (!DemoAccount.Enabled(config)) return Results.NotFound();
            if (!gate.TryTake(RateLimits.Ip(request.HttpContext)))
            {
                return Results.Json(request.Error("demo.limit"), statusCode: StatusCodes.Status429TooManyRequests);
            }
            var result = await auth.CreateDemoAsync(body?.TzOffsetMinutes ?? -300);
            return result.Token is null
                ? ToResult(request, result)
                : Results.Ok(new { token = result.Token, email = result.Email, demo = true });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/logout", async (HttpRequest request, AuthService auth) =>
        {
            await auth.LogoutAsync(request);
            return Results.Ok();
        });

        // Sahifa ochilganda frontend saqlangan token hali yaroqlimi deb shu
        // yerdan so'raydi (muddati o'tgan yoki "Chiqish" qilingan bo'lishi mumkin).
        app.MapGet("/api/auth/me", async (HttpRequest request, AuthService auth) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Results.Json(request.Error("auth.not_logged_in"), statusCode: StatusCodes.Status401Unauthorized);
            return Results.Ok(new { email = user.Email, signIn = await auth.GetSignInMethodsAsync(user) });
        });
    }
}
