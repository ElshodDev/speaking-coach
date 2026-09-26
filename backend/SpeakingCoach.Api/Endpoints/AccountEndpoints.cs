using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record DeleteAccountRequest(string ConfirmEmail);

/// <summary>
/// Foydalanuvchining o'z ma'lumotlari ustidan nazorati: hammasini yuklab
/// olish (JSON) va hisobni butunlay o'chirish. Maxfiylik siyosatidagi
/// va'daning texnik tomoni.
/// </summary>
public static class AccountEndpoints
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

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
            if (AuthService.NormalizeEmail(body.ConfirmEmail ?? "") != user.Email)
            {
                return Results.BadRequest(request.Error("account.confirm_mismatch"));
            }

            var audioIds = await db.Activities
                .Where(a => a.UserId == user.Id && a.Type == ActivityType.Speaking)
                .Select(a => a.Id)
                .ToListAsync();

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            foreach (var id in audioIds)
            {
                var file = Path.Combine(uploadsPath, $"{id}.webm");
                try
                {
                    if (File.Exists(file)) File.Delete(file);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Audio faylni o'chirib bo'lmadi: {File}", file);
                }
            }

            logger.LogInformation("Hisob o'chirildi: {UserId}", user.Id);
            return Results.Ok(new { deleted = true });
        }).RequireRateLimiting("auth");
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
