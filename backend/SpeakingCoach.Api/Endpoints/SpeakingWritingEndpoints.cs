using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

// Writing so'rov shakli — Speaking'dan farqli, JSON body sifatida keladi
// (audio fayl yo'q, shuning uchun multipart/form-data shart emas).
public record WritingSubmitRequest(string Topic, string Text, string? Level = null);

public static class SpeakingWritingEndpoints
{
    /// <summary>Mavzu — bitta savol yoki sarlavha; uzun matn promptni shishirmasin.</summary>
    public const int MaxTopicChars = 300;

    /// <summary>Speaking: bitta yozuv (8 MB) + forma maydonlari.</summary>
    private const long SpeakingBodyBytes = Uploads.MaxRecordingBytes + Uploads.FormOverheadBytes;

    public static void MapSpeakingWritingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/speaking/submit", async (
            HttpRequest request,
            ISpeakingEvaluationService evaluationService,
            AppDbContext db,
            AuthService auth,
            ReviewService reviews,
            AiQuotaService quotas,
            ILogger<Program> logger) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(request.Error("speaking.multipart"));
            }
            // Hajm — formani o'qishdan OLDIN (Content-Length bo'yicha).
            if (Uploads.DeclaredTooLarge(request, SpeakingBodyBytes))
            {
                return Results.Json(request.Error("audio.too_big", Uploads.MaxRecordingBytes / Uploads.Mb), statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            // "Katta so'rov" joyi — audio xotiraga o'qilishidan (va base64'dan) OLDIN.
            var ct = request.HttpContext.RequestAborted;
            var form = await Uploads.ReadFormAsync(request, ct);
            using var slot = await Uploads.EnterIfLargeAsync(request, ct);
            var audioFile = form.Files.GetFile("audio");
            var topic = form["topic"].ToString();
            var level = LearnerLevel.Normalize(form["level"].ToString());
            // Ravonlik o'lchovlari uchun: yozuv uzunligi va uzun pauzalar (brauzer audio'dan hisoblaydi).
            var (seconds, longPauses, pauseSeconds) = FluencyMetrics.Sanitize(
                form["seconds"].ToString(), form["longPauses"].ToString(), form["pauseSeconds"].ToString());

            if (audioFile is null || audioFile.Length == 0)
            {
                return Results.BadRequest(request.Error("speaking.no_audio"));
            }

            if (string.IsNullOrWhiteSpace(topic))
            {
                return Results.BadRequest(request.Error("topic.empty"));
            }
            if (topic.Length > MaxTopicChars)
            {
                return Results.BadRequest(request.Error("topic.too_long", MaxTopicChars));
            }

            if (audioFile.Length > Uploads.MaxRecordingBytes)
            {
                return Results.BadRequest(request.Error("audio.too_big", Uploads.MaxRecordingBytes / Uploads.Mb));
            }

            // Kunlik AI limiti — Gemini'ga murojaatdan OLDIN.
            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;

            var submissionId = Guid.NewGuid();

            var audioBytes = await Uploads.ReadAsync(audioFile, ct);

            // Saqlash ikki shartga bog'liq:
            // 1) ?save=false emas — barqarorlik testi tarixni "axlat" bilan
            //    to'ldirmasligi uchun;
            // 2) foydalanuvchi tizimga kirgan — aks holda yozuvni kimning
            //    tarixiga qo'yishni bilmaymiz (mehmon ham baholash oladi,
            //    lekin natija saqlanmaydi).
            var saveRequested = !bool.TryParse(request.Query["save"], out var saveFlag) || saveFlag;
            var userId = await auth.GetCurrentUserIdAsync(request);
            var shouldSave = saveRequested && userId is not null;

            // Audio diskka yozilmaydi: faqat baholash uchun kerak, keyin hech
            // qayerda ishlatilmaydi (shaxsiy ma'lumot — keraksiz saqlanmaydi).

            var newCards = 0;
            try
            {
                logger.LogInformation("Submission {Id}: Gemini'ga yuborilmoqda ({Size} bayt)", submissionId, audioBytes.Length);
                var evaluation = await evaluationService.EvaluateAsync(topic, audioBytes, audioFile.ContentType, level);
                // AI'siz, transkript va vaqtdan: daqiqasiga so'z, to'ldiruvchi so'zlar, takrorlar, pauzalar.
                var metrics = FluencyMetrics.Analyze(evaluation.Transcript, seconds, longPauses, pauseSeconds);

                // Natijani bazaga yozamiz. Bu Gemini so'rovidan KEYIN qilinadi —
                // agar Gemini xato qaytarsa (masalan 503), hech narsa saqlanmaydi,
                // shuning uchun tarixda faqat muvaffaqiyatli urinishlar ko'rinadi.
                if (shouldSave)
                {
                    db.Activities.Add(new Activity
                    {
                        Id = submissionId,
                        Type = ActivityType.Speaking,
                        UserId = userId,
                        CreatedAtUtc = DateTime.UtcNow,
                        PromptData = System.Text.Json.JsonSerializer.Serialize(new { topic }),
                        ResponseData = FluencyMetrics.WithMetrics(evaluation, metrics),
                    });
                    // Tuzatishlar avtomatik takrorlash kartalariga aylanadi —
                    // urinish bilan bitta tranzaksiyada saqlanadi.
                    newCards = await reviews.AddCardsAsync(
                        userId!.Value, ActivityType.Speaking, ReviewCardFactory.FromCorrections(evaluation.TopCorrections));
                    await db.SaveChangesAsync();
                }

                await quotas.RecordAsync(request, AiKind.Exercise);
                return Results.Ok(new { submissionId, evaluation, metrics, saved = shouldSave, newCards });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                // Texnik sabab faqat logga yoziladi; foydalanuvchiga — uning tilidagi
                // tushunarli xabar. "AI band" (AiBusyException) — markaziy 503.
                logger.LogError(ex, "Submission {Id}: xato", submissionId);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(SpeakingBodyBytes);

        app.MapGet("/api/speaking/history", (HttpRequest request, AppDbContext db, AuthService auth) =>
            HistoryQueries.GetHistoryAsync(ActivityType.Speaking, request, db, auth));

        // Ravonlik dinamikasi (Progress grafigi): Speaking mashqi, Speaking imtihoni va AI suhbat —
        // oxirgi urinishlardagi o'lchovlar, eski → yangi. Faqat o'lchovli yozuvlar ("metrics" kaliti
        // bazada tekshiriladi — boshqa mock natijalarining katta JSON'i o'qilmaydi).
        app.MapGet("/api/speaking/fluency", async (HttpRequest request, AppDbContext db, AuthService auth, int take = 20) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Results.Ok(Array.Empty<FluencyPoint>());
            ActivityType[] types = [ActivityType.Speaking, ActivityType.MockExam, ActivityType.Conversation];
            var rows = await db.Activities
                .Where(a => a.UserId == userId && types.Contains(a.Type) && EF.Functions.JsonExists(a.ResponseData, "metrics"))
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(Math.Clamp(take, 1, 50))
                .Select(a => new { a.CreatedAtUtc, a.Type, a.ResponseData })
                .ToListAsync();
            var points = rows
                .OrderBy(r => r.CreatedAtUtc)
                .Select(r => (r.CreatedAtUtc, r.Type, M: FluencyMetrics.FromResponse(r.ResponseData)))
                .Where(x => x.M?.Wpm is not null)
                .Select(x => new FluencyPoint(x.CreatedAtUtc, x.M!.Wpm, x.M.FillersPerMinute, x.M.LongPauses,
                    x.Type switch { ActivityType.MockExam => "mock", ActivityType.Conversation => "talk", _ => "speaking" }))
                .ToList();
            return Results.Ok(points);
        });

        app.MapPost("/api/writing/submit", async (
            WritingSubmitRequest body,
            HttpRequest request,
            IWritingEvaluationService evaluationService,
            AppDbContext db,
            AuthService auth,
            ReviewService reviews,
            AiQuotaService quotas,
            ILogger<Program> logger,
            bool save = true) =>
        {
            if (string.IsNullOrWhiteSpace(body.Topic))
            {
                return Results.BadRequest(request.Error("topic.empty"));
            }
            if (body.Topic.Length > MaxTopicChars)
            {
                return Results.BadRequest(request.Error("topic.too_long", MaxTopicChars));
            }

            if (string.IsNullOrWhiteSpace(body.Text))
            {
                return Results.BadRequest(request.Error("text.empty"));
            }

            // Juda qisqa matn Gemini'dan mazmunli baholash olishga yetarli emas;
            // juda uzun matn esa prompt hajmini keraksiz shishiradi.
            const int minChars = 20;
            const int maxChars = 5000;
            if (body.Text.Length < minChars)
            {
                return Results.BadRequest(request.Error("text.too_short", minChars));
            }
            if (body.Text.Length > maxChars)
            {
                return Results.BadRequest(request.Error("text.too_long", maxChars));
            }

            // Kunlik AI limiti — Gemini'ga murojaatdan OLDIN.
            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;

            var submissionId = Guid.NewGuid();
            var userId = await auth.GetCurrentUserIdAsync(request);
            var shouldSave = save && userId is not null;

            var newCards = 0;
            try
            {
                logger.LogInformation("Writing submission {Id}: Gemini'ga yuborilmoqda ({Length} belgi)", submissionId, body.Text.Length);
                var evaluation = await evaluationService.EvaluateAsync(body.Topic, body.Text, LearnerLevel.Normalize(body.Level));

                if (shouldSave)
                {
                    db.Activities.Add(new Activity
                    {
                        Id = submissionId,
                        Type = ActivityType.Writing,
                        UserId = userId,
                        CreatedAtUtc = DateTime.UtcNow,
                        PromptData = System.Text.Json.JsonSerializer.Serialize(new { topic = body.Topic, text = body.Text }),
                        ResponseData = System.Text.Json.JsonSerializer.Serialize(evaluation),
                    });
                    // Tuzatishlar avtomatik takrorlash kartalariga aylanadi —
                    // urinish bilan bitta tranzaksiyada saqlanadi.
                    newCards = await reviews.AddCardsAsync(
                        userId!.Value, ActivityType.Writing, ReviewCardFactory.FromCorrections(evaluation.TopCorrections));
                    await db.SaveChangesAsync();
                }

                await quotas.RecordAsync(request, AiKind.Exercise);
                return Results.Ok(new { submissionId, evaluation, saved = shouldSave, newCards });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "Writing submission {Id}: xato", submissionId);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapGet("/api/writing/history", (HttpRequest request, AppDbContext db, AuthService auth) =>
            HistoryQueries.GetHistoryAsync(ActivityType.Writing, request, db, auth));
    }
}
