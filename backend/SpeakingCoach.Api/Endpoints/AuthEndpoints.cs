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
                return Results.Json(request.Error(result.Error, result.ErrorArgs ?? []), statusCode: result.Status);
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
            .RequireRateLimiting("auth");

        app.MapPost("/api/auth/forgot", async (EmailRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.ForgotPasswordAsync(body.Email, Texts.LangOf(request))))
            .RequireRateLimiting("auth");

        app.MapPost("/api/auth/reset", async (ResetRequest body, HttpRequest request, AuthService auth) =>
            ToResult(request, await auth.ResetPasswordAsync(body.Email, body.Code, body.NewPassword)))
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
        // yuboriladi; bot bilan ulangan hisob bo'lsa — parolsiz kiriladi.
        app.MapPost("/api/auth/telegram", async (TelegramAuthRequest body, HttpRequest request, TelegramOptions options, AppDbContext db, AuthService auth) =>
        {
            var tgUserId = TelegramWebAppAuth.Validate(body.InitData, options.Token, DateTime.UtcNow);
            if (tgUserId is null) return Results.Json(request.Error("auth.telegram_invalid"), statusCode: StatusCodes.Status401Unauthorized);
            // Shaxsiy chatda chat ID = foydalanuvchi ID.
            var account = await db.TelegramAccounts.FirstOrDefaultAsync(a => a.ChatId == tgUserId.Value);
            var result = account is null ? null : await auth.SignInLinkedAsync(account.UserId);
            return result is null
                ? Results.Json(request.Error("auth.telegram_not_linked"), statusCode: StatusCodes.Status404NotFound)
                : ToResult(request, result);
        }).RequireRateLimiting("auth");

        // Frontend "Parolni unutdim" va "Google bilan kirish" tugmalarini faqat
        // tegishli xizmat sozlangan bo'lsa ko'rsatadi. Client ID maxfiy emas.
        app.MapGet("/api/auth/config", (AuthService auth, IGoogleSignIn google, IConfiguration config) =>
            Results.Ok(new { emailVerification = auth.VerificationRequired, googleClientId = google.ClientId, demo = DemoAccount.Enabled(config) }));

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
            return user is null
                ? Results.Json(request.Error("auth.not_logged_in"), statusCode: StatusCodes.Status401Unauthorized)
                : Results.Ok(new { email = user.Email });
        });
    }
}
