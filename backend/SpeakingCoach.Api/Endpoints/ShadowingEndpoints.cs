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
    private const long MaxBodyBytes = MaxAudioBytes + Uploads.FormOverheadBytes;

    public static void MapShadowingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shadowing", async (ShadowingService lessons) => Results.Ok(await lessons.SummariesAsync()));

        // Mening videolarim: foydalanuvchining shaxsiy (botda qo'shgan) darslari. Mehmon — bo'sh.
        app.MapGet("/api/shadowing/mine", async (HttpRequest request, AuthService auth, ShadowingService lessons) =>
            await auth.GetCurrentUserIdAsync(request) is { } uid ? Results.Ok(await lessons.MineAsync(uid)) : Results.Ok(Array.Empty<ShadowingSummary>()));

        // Foydalanuvchining natijalari: qaysi darslar bajarilgan va eng yaxshi ball (mehmon — bo'sh).
        app.MapGet("/api/shadowing/progress", async (HttpRequest request, AuthService auth, PracticeStore store) =>
            await auth.GetCurrentUserIdAsync(request) is { } uid ? Results.Ok(await store.ShadowingProgressAsync(uid)) : Results.Ok(Array.Empty<ShadowingProgress>()));

        app.MapGet("/api/shadowing/{id}", async (string id, HttpRequest request, AuthService auth, ShadowingService lessons) =>
            await lessons.FindAsync(id, await auth.GetCurrentUserIdAsync(request)) is { } l ? Results.Ok(l) : Results.NotFound(request.Error("shadowing.not_found")));

        app.MapPost("/api/shadowing/check", async (
            HttpRequest request,
            AuthService auth,
            ShadowingService lessons,
            IShadowingCoach coach,
            AiQuotaService quotas,
            ILogger<Program> logger) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("shadowing.bad_audio"));
            if (Uploads.DeclaredTooLarge(request, MaxBodyBytes))
                return Results.Json(request.Error("shadowing.bad_audio"), statusCode: StatusCodes.Status413PayloadTooLarge);
            var ct = request.HttpContext.RequestAborted;
            // "Katta so'rov" joyi — audio xotiraga o'qilishidan oldin.
            var form = await Uploads.ReadFormAsync(request, ct);
            using var slot = await Uploads.EnterIfLargeAsync(request, ct);
            var audio = form.Files.GetFile("audio");
            if (audio is null || audio.Length == 0 || audio.Length > MaxAudioBytes) return Results.BadRequest(request.Error("shadowing.bad_audio"));

            var lesson = await lessons.FindAsync(form["lessonId"].ToString(), await auth.GetCurrentUserIdAsync(request));
            if (lesson is null || !int.TryParse(form["line"], out var index) || index < 0 || index >= lesson.Lines.Count)
            {
                return Results.NotFound(request.Error("shadowing.not_found"));
            }

            var limited = await quotas.CheckAsync(request, AiKind.Shadowing);
            if (limited is not null) return limited;

            var bytes = await Uploads.ReadAsync(audio, ct);
            try
            {
                var result = await coach.CheckAsync(lesson.Lines[index].Text, bytes, audio.ContentType, RequestTexts.LangOf(request), ct);
                await quotas.RecordAsync(request, AiKind.Shadowing);
                return Results.Ok(result);
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogWarning(ex, "Shadowing tekshiruvi bajarilmadi: {Lesson} #{Line}", lesson.Id, index);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(MaxBodyBytes);

        // Dars yakunlandi: hisobi bor foydalanuvchiga faoliyat (XP va seriya).
        // Bir dars — kuniga bitta yozuv (qayta-qayta bosib XP yig'ib bo'lmaydi).
        app.MapPost("/api/shadowing/{id}/done", async (
            string id,
            ShadowingDoneRequest body,
            HttpRequest request,
            ShadowingService lessons,
            AuthService auth,
            PracticeStore store) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            var lesson = await lessons.FindAsync(id, userId);
            if (lesson is null) return Results.NotFound(request.Error("shadowing.not_found"));
            if (userId is null) return Results.Ok(new { saved = false });
            return Results.Ok(new { saved = await store.SaveShadowingDoneAsync(userId.Value, lesson, body.Lines, body.Average) });
        }).RequireRateLimiting(RateLimits.WritePolicy);
    }
}
