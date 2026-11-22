using Microsoft.AspNetCore.Mvc;
namespace SpeakingCoach.Api.Endpoints;

public record DeleteAccountRequest(string ConfirmEmail);
public record SetPasswordRequest(string? CurrentPassword, string NewPassword);
public record EmailChangeRequest(string Email);
public record EmailConfirmRequest(string Email, string Code);

/// <summary>
/// Foydalanuvchining o'z ma'lumotlari ustidan nazorati: hammasini yuklab
/// olish (JSON) va hisobni butunlay o'chirish. Maxfiylik siyosatidagi
/// va'daning texnik tomoni.
/// </summary>
public static class AccountEndpoints
{
    /// <summary>Hisobni o'chirishni tasdiqlash so'zi (emaili sintetik — Telegram/demo — hisoblar uchun).</summary>
    public const string DeleteWord = "DELETE";

    /// <summary>
    /// O'chirishni tasdiqlash: haqiqiy emailli hisob — emailni aynan yozadi;
    /// emaili sintetik hisob (foydalanuvchi o'z "emailini" bilmaydi) — "DELETE"
    /// (katta-kichik harf farqi yo'q; sintetik emailning o'zi ham qabul qilinadi).
    /// </summary>
    public static bool DeleteConfirmed(string userEmail, string? confirmation)
    {
        var typed = (confirmation ?? "").Trim();
        if (AuthService.NormalizeEmail(typed) == userEmail) return true;
        return AuthService.IsSyntheticEmail(userEmail) && string.Equals(typed, DeleteWord, StringComparison.OrdinalIgnoreCase);
    }

    private static IResult ToResult(HttpRequest request, AuthResult result, object ok) =>
        result.Error is not null
            ? Results.Json(request.ErrorWithCode(result.Error, result.ErrorArgs ?? []), statusCode: result.Status)
            : Results.Ok(ok);

    public static void MapAccountEndpoints(this IEndpointRouteBuilder app, string uploadsPath)
    {
        static IResult Unauthorized(HttpRequest request) =>
            Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);

        // Barcha ma'lumotlar bitta JSON faylda (tuzilishi — AccountDataService.ExportJsonAsync).
        app.MapGet("/api/account/export", async (HttpRequest request, AuthService auth, AccountDataService data) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);

            var bytes = await data.ExportJsonAsync(user);
            return Results.File(bytes, "application/json", $"speaking-coach-{DateTime.UtcNow:yyyy-MM-dd}.json");
        });

        // Hisobni o'chirish: tasodifan bosib yuborilmasligi uchun email qayta
        // kiritiladi. Foydalanuvchi o'chirilganda bazadagi hamma narsa
        // (mashqlar, kartalar, sessiyalar, kodlar, limitlar) cascade bilan
        // o'chadi; diskdagi ovoz yozuvlarini esa alohida o'chiramiz.
        // DELETE so'rovida tana (body) avtomatik o'qilmaydi — [FromBody] majburiy,
        // aks holda ASP.NET ishga tushishda BUTUN marshrutlashni yiqitadi.
        app.MapDelete("/api/account", async ([FromBody] DeleteAccountRequest body, HttpRequest request, AuthService auth, AccountDataService data, ILogger<Program> logger) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (!DeleteConfirmed(user.Email, body.ConfirmEmail))
            {
                return Results.BadRequest(request.ErrorWithCode(
                    AuthService.IsSyntheticEmail(user.Email) ? "account.confirm_delete_word" : "account.confirm_mismatch"));
            }
            // Qaytarib bo'lmaydigan amal: o'g'irlangan (eski) token bilan hisob o'chirilmasin —
            // yaqinda qayta kirgan bo'lish shart (Telegram Mini App har ochilishda yangi sessiya oladi).
            if (!DemoAccount.IsDemo(user.Email) && !await auth.HasRecentLoginAsync(request))
            {
                return Results.Json(request.ErrorWithCode("account.reauth", (int)AuthService.RecentLoginWindow.TotalMinutes),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var audioIds = await data.DeleteAccountAsync(user);

            foreach (var id in audioIds)
            {
                var file = Path.Combine(uploadsPath, $"{id}.webm");
                try
                {
                    if (File.Exists(file)) File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning(ex, "Audio faylni o'chirib bo'lmadi: {File}", file);
                }
            }

            logger.LogInformation("Hisob o'chirildi: {UserId}", user.Id);
            return Results.Ok(new { deleted = true });
        }).RequireRateLimiting("auth");

        // Parol o'rnatish (Google/kod/Telegram bilan ochilgan hisob) yoki almashtirish.
        app.MapPost("/api/account/password", async (SetPasswordRequest body, HttpRequest request, AuthService auth) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var result = await auth.SetPasswordAsync(user, body.CurrentPassword, body.NewPassword, request);
            return ToResult(request, result, new { ok = true });
        }).RequireRateLimiting("write");

        // Email qo'shish/almashtirish, 1-qadam: yangi manzilga kod.
        app.MapPost("/api/account/email/request", async (EmailChangeRequest body, HttpRequest request, AuthService auth) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var result = await auth.RequestEmailChangeAsync(user, body.Email, RequestTexts.LangOf(request));
            return ToResult(request, result, new { ok = true, email = result.Email });
        }).RequireRateLimiting("code");

        // 2-qadam: kod to'g'ri — email almashadi (tasdiqlangan).
        app.MapPost("/api/account/email/confirm", async (EmailConfirmRequest body, HttpRequest request, AuthService auth) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var result = await auth.ConfirmEmailChangeAsync(user, body.Email, body.Code);
            return ToResult(request, result, new { ok = true, email = result.Email });
        }).RequireRateLimiting("write");
    }
}
