using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Endpoints;

public record ShadowingDoneRequest(int Lines, int? Average);

/// <summary>
/// Shadowing: darslar ro'yxati, bitta dars, gap talaffuzini AI bilan
/// tekshirish (kunlik alohida limit) va darsni yakunlash (XP, seriya).
/// Mehmonlar ham mashq qila oladi; natija faqat hisobi borlarga saqlanadi.
/// </summary>
public static class ShadowingEndpoints
{
    public const long MaxAudioBytes = 2 * 1024 * 1024;

    public static string? LessonIdOf(string promptJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(promptJson);
            return doc.RootElement.TryGetProperty("lessonId", out var v) ? v.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void MapShadowingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shadowing", async (ShadowingService lessons) => Results.Ok(await lessons.SummariesAsync()));

        // Foydalanuvchining natijalari: qaysi darslar bajarilgan va eng yaxshi ball (mehmon — bo'sh).
        app.MapGet("/api/shadowing/progress", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Ok(Array.Empty<ShadowingProgress>());
            var rows = await db.Activities
                .Where(a => a.UserId == userId && a.Type == ActivityType.Shadowing)
                .Select(a => new { a.PromptData, a.ResponseData })
                .ToListAsync();
            return Results.Ok(ShadowingService.ProgressOf(rows.Select(r => (r.PromptData, r.ResponseData))));
        });

        app.MapGet("/api/shadowing/{id}", async (string id, HttpRequest request, ShadowingService lessons) =>
            await lessons.FindAsync(id) is { } l ? Results.Ok(l) : Results.NotFound(request.Error("shadowing.not_found")));

        app.MapPost("/api/shadowing/check", async (
            HttpRequest request,
            ShadowingService lessons,
            IShadowingCoach coach,
            AiQuotaService quotas,
            ILogger<Program> logger) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("shadowing.bad_audio"));
            var form = await request.ReadFormAsync();
            var audio = form.Files.GetFile("audio");
            if (audio is null || audio.Length == 0 || audio.Length > MaxAudioBytes) return Results.BadRequest(request.Error("shadowing.bad_audio"));

            var lesson = await lessons.FindAsync(form["lessonId"].ToString());
            if (lesson is null || !int.TryParse(form["line"], out var index) || index < 0 || index >= lesson.Lines.Count)
            {
                return Results.NotFound(request.Error("shadowing.not_found"));
            }

            var limited = await quotas.CheckAsync(request, AiKind.Shadowing);
            if (limited is not null) return limited;

            using var ms = new MemoryStream();
            await audio.CopyToAsync(ms);
            try
            {
                var result = await coach.CheckAsync(lesson.Lines[index].Text, ms.ToArray(), audio.ContentType, Texts.LangOf(request), request.HttpContext.RequestAborted);
                await quotas.RecordAsync(request, AiKind.Shadowing);
                return Results.Ok(result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Shadowing tekshiruvi bajarilmadi: {Lesson} #{Line}", lesson.Id, index);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting("ai");

        // Dars yakunlandi: hisobi bor foydalanuvchiga faoliyat (XP va seriya).
        // Bir dars — kuniga bitta yozuv (qayta-qayta bosib XP yig'ib bo'lmaydi).
        app.MapPost("/api/shadowing/{id}/done", async (
            string id,
            ShadowingDoneRequest body,
            HttpRequest request,
            ShadowingService lessons,
            AuthService auth,
            AppDbContext db) =>
        {
            var lesson = await lessons.FindAsync(id);
            if (lesson is null) return Results.NotFound(request.Error("shadowing.not_found"));
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Ok(new { saved = false });

            var since = DateTime.UtcNow.AddHours(-20);
            var prompt = JsonSerializer.Serialize(new { lessonId = lesson.Id, title = lesson.Title, level = lesson.Level, kind = lesson.Kind });
            var recent = await db.Activities
                .Where(a => a.UserId == userId && a.Type == ActivityType.Shadowing && a.CreatedAtUtc > since)
                .Select(a => a.PromptData)
                .ToListAsync();
            // jsonb o'z formatida qaytaradi (bo'shliqlar bilan) — matn qidiruvi emas, JSON o'qiymiz.
            if (recent.Any(p => LessonIdOf(p) == lesson.Id))
            {
                return Results.Ok(new { saved = false });
            }

            db.Activities.Add(new Activity
            {
                Id = Guid.NewGuid(),
                Type = ActivityType.Shadowing,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                PromptData = prompt,
                ResponseData = JsonSerializer.Serialize(new
                {
                    lines = Math.Clamp(body.Lines, 0, lesson.Lines.Count),
                    total = lesson.Lines.Count,
                    average = body.Average is int a ? Math.Clamp(a, 0, 100) : (int?)null,
                }),
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { saved = true });
        });
    }
}
