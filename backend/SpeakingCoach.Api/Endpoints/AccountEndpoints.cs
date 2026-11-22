using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

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

        // Barcha ma'lumotlar bitta JSON faylda. jsonb ustunlar (mashq matni va
        // baholar) ichma-ich JSON sifatida — matn qatori sifatida emas.
        app.MapGet("/api/account/export", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);

            var activities = await db.Activities
                .Where(a => a.UserId == user.Id)
                .OrderBy(a => a.CreatedAtUtc)
                .Select(a => new { a.Id, a.Type, a.CreatedAtUtc, a.PromptData, a.ResponseData })
                .ToListAsync();
            var cards = await db.ReviewCards
                .Where(c => c.UserId == user.Id)
                .OrderBy(c => c.CreatedAtUtc)
                .ToListAsync();
            var logs = await db.ReviewLogs
                .Where(l => l.UserId == user.Id)
                .OrderBy(l => l.ReviewedAtUtc)
                .Select(l => new { l.CardId, l.Grade, l.ReviewedAtUtc })
                .ToListAsync();

            var taught = await db.Groups.Where(g => g.TeacherId == user.Id).Select(g => new { g.Id, g.Name, g.CreatedAtUtc }).ToListAsync();
            var taughtIds = taught.Select(g => g.Id).ToList();
            var assignments = await db.Assignments
                .Where(a => taughtIds.Contains(a.GroupId))
                .OrderBy(a => a.CreatedAtUtc)
                .Select(a => new { a.Id, a.GroupId, a.Kind, a.Target, a.Instructions, a.DueAtUtc, a.CreatedAtUtc })
                .ToListAsync();
            var memberOf = await db.GroupMembers.Where(m => m.UserId == user.Id).Select(m => new { m.GroupId, m.JoinedAtUtc }).ToListAsync();
            var feedback = await db.Feedbacks
                .Where(f => f.UserId == user.Id)
                .OrderBy(f => f.CreatedAtUtc)
                .Select(f => new { f.Id, f.Kind, f.Message, f.Page, f.Lang, f.CreatedAtUtc, f.Ip })
                .ToListAsync();
            var telegram = await db.TelegramAccounts
                .Where(t => t.UserId == user.Id)
                .Select(t => new { t.ChatId, t.Username, t.Lang, t.ReminderHour, t.TzOffsetMinutes, t.LinkedAtUtc, t.LastSeenAtUtc, t.LastReminderDate })
                .ToListAsync();
            var sessions = await db.Sessions
                .Where(x => x.UserId == user.Id)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new { x.CreatedAtUtc, x.ExpiresAtUtc })
                .ToListAsync();
            var aiUsage = await db.AiUsages
                .Where(u => u.UserId == user.Id)
                .OrderBy(u => u.Day)
                .Select(u => new { u.Day, u.Exercises, u.Words, u.Shadowing })
                .ToListAsync();
            var authored = await db.MockTests
                .Where(m => m.CreatedByUserId == user.Id)
                .OrderBy(m => m.CreatedAtUtc)
                .Select(m => new { m.Id, m.Exam, m.Module, m.Variant, m.Title, m.Status, m.CreatedAtUtc, m.Payload })
                .ToListAsync();

            var export = new
            {
                exportedAtUtc = DateTime.UtcNow,
                account = new
                {
                    user.Email,
                    user.CreatedAtUtc,
                    user.EmailVerifiedAtUtc,
                    user.DisplayName,
                    user.Level,
                    user.ShowOnLeaderboard,
                    user.Goal,
                    user.TargetScore,
                    user.ExamDate,
                    user.DailyMinutes,
                    user.OnboardedAtUtc,
                    user.SignupMethod,
                    hasPassword = !string.IsNullOrEmpty(user.PasswordHash),
                },
                activities = activities.Select(a => new
                {
                    a.Id,
                    type = a.Type.ToString(),
                    a.CreatedAtUtc,
                    prompt = ParseJson(a.PromptData),
                    result = ParseJson(a.ResponseData),
                }),
                reviewCards = cards.Select(c => new
                {
                    c.Id,
                    kind = c.Kind.ToString(),
                    source = c.Source?.ToString(),
                    c.Front,
                    c.Back,
                    c.Note,
                    c.CreatedAtUtc,
                    c.DueAtUtc,
                    c.IntervalDays,
                    c.Repetitions,
                    c.Lapses,
                }),
                reviewLog = logs,
                groupsTaught = taught,
                assignmentsCreated = assignments,
                groupMemberships = memberOf,
                mockTestsAuthored = authored.Select(m => new { m.Id, m.Exam, m.Module, m.Variant, m.Title, m.Status, m.CreatedAtUtc, content = ParseJson(m.Payload) }),
                feedback,
                telegram,
                // Faqat vaqtlar — token xeshlari (sir) eksportga kirmaydi.
                sessions,
                aiUsage,
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(export, Pretty);
            return Results.File(bytes, "application/json", $"speaking-coach-{DateTime.UtcNow:yyyy-MM-dd}.json");
        });

        // Hisobni o'chirish: tasodifan bosib yuborilmasligi uchun email qayta
        // kiritiladi. Foydalanuvchi o'chirilganda bazadagi hamma narsa
        // (mashqlar, kartalar, sessiyalar, kodlar, limitlar) cascade bilan
        // o'chadi; diskdagi ovoz yozuvlarini esa alohida o'chiramiz.
        // DELETE so'rovida tana (body) avtomatik o'qilmaydi — [FromBody] majburiy,
        // aks holda ASP.NET ishga tushishda BUTUN marshrutlashni yiqitadi.
        app.MapDelete("/api/account", async ([FromBody] DeleteAccountRequest body, HttpRequest request, AuthService auth, AppDbContext db, ILogger<Program> logger) =>
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

            var audioIds = await db.Activities
                .Where(a => a.UserId == user.Id && a.Type == ActivityType.Speaking)
                .Select(a => a.Id)
                .ToListAsync();

            // Shaxsiy (faqat o'zi ko'radigan) darslar hisob bilan birga o'chadi; chop etilganlar bankda qoladi.
            await db.MockTests.Where(m => m.CreatedByUserId == user.Id && m.Status == MockTestStatus.Personal).ExecuteDeleteAsync();
            db.Users.Remove(user);
            await db.SaveChangesAsync();

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

    private static JsonElement? ParseJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
