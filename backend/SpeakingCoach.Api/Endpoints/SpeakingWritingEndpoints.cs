using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

// Writing so'rov shakli — Speaking'dan farqli, JSON body sifatida keladi
// (audio fayl yo'q, shuning uchun multipart/form-data shart emas).
public record WritingSubmitRequest(string Topic, string Text, string? Level = null);

public static class SpeakingWritingEndpoints
{
    public static void MapSpeakingWritingEndpoints(this IEndpointRouteBuilder app, string uploadsPath)
    {
        app.MapPost("/api/speaking/submit", async (
            HttpRequest request,
            ISpeakingEvaluationService evaluationService,
            AppDbContext db,
            AuthService auth,
            ReviewService reviews,
            ILogger<Program> logger) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(request.Error("speaking.multipart"));
            }

            var form = await request.ReadFormAsync();
            var audioFile = form.Files.GetFile("audio");
            var topic = form["topic"].ToString();
            var level = LearnerLevel.Normalize(form["level"].ToString());

            if (audioFile is null || audioFile.Length == 0)
            {
                return Results.BadRequest(request.Error("speaking.no_audio"));
            }

            if (string.IsNullOrWhiteSpace(topic))
            {
                return Results.BadRequest(request.Error("topic.empty"));
            }

            const long maxBytes = 10 * 1024 * 1024; // 10 MB
            if (audioFile.Length > maxBytes)
            {
                return Results.BadRequest(request.Error("speaking.too_big"));
            }

            var submissionId = Guid.NewGuid();

            byte[] audioBytes;
            using (var memoryStream = new MemoryStream())
            {
                await audioFile.CopyToAsync(memoryStream);
                audioBytes = memoryStream.ToArray();
            }

            // Saqlash ikki shartga bog'liq:
            // 1) ?save=false emas — barqarorlik testi tarixni "axlat" bilan
            //    to'ldirmasligi uchun;
            // 2) foydalanuvchi tizimga kirgan — aks holda yozuvni kimning
            //    tarixiga qo'yishni bilmaymiz (mehmon ham baholash oladi,
            //    lekin natija saqlanmaydi).
            var saveRequested = !bool.TryParse(request.Query["save"], out var saveFlag) || saveFlag;
            var userId = await auth.GetCurrentUserIdAsync(request);
            var shouldSave = saveRequested && userId is not null;

            if (shouldSave)
            {
                await File.WriteAllBytesAsync(Path.Combine(uploadsPath, $"{submissionId}.webm"), audioBytes);
            }

            var newCards = 0;
            try
            {
                logger.LogInformation("Submission {Id}: Gemini'ga yuborilmoqda ({Size} bayt)", submissionId, audioBytes.Length);
                var evaluation = await evaluationService.EvaluateAsync(topic, audioBytes, audioFile.ContentType, level);

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
                        ResponseData = System.Text.Json.JsonSerializer.Serialize(evaluation),
                    });
                    // Tuzatishlar avtomatik takrorlash kartalariga aylanadi —
                    // urinish bilan bitta tranzaksiyada saqlanadi.
                    newCards = await reviews.AddCardsAsync(
                        userId!.Value, ActivityType.Speaking, ReviewCardFactory.FromCorrections(evaluation.TopCorrections));
                    await db.SaveChangesAsync();
                }

                return Results.Ok(new { submissionId, evaluation, saved = shouldSave, newCards });
            }
            catch (Exception ex)
            {
                // Texnik sabab (masalan, Gemini band) faqat logga yoziladi;
                // foydalanuvchiga — uning tilidagi tushunarli xabar.
                logger.LogError(ex, "Submission {Id}: xato", submissionId);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting("ai");

        app.MapGet("/api/speaking/history", (HttpRequest request, AppDbContext db, AuthService auth) =>
            HistoryQueries.GetHistoryAsync(ActivityType.Speaking, request, db, auth));

        app.MapPost("/api/writing/submit", async (
            WritingSubmitRequest body,
            HttpRequest request,
            IWritingEvaluationService evaluationService,
            AppDbContext db,
            AuthService auth,
            ReviewService reviews,
            ILogger<Program> logger,
            bool save = true) =>
        {
            if (string.IsNullOrWhiteSpace(body.Topic))
            {
                return Results.BadRequest(request.Error("topic.empty"));
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

                return Results.Ok(new { submissionId, evaluation, saved = shouldSave, newCards });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Writing submission {Id}: xato", submissionId);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting("ai");

        app.MapGet("/api/writing/history", (HttpRequest request, AppDbContext db, AuthService auth) =>
            HistoryQueries.GetHistoryAsync(ActivityType.Writing, request, db, auth));
    }
}
