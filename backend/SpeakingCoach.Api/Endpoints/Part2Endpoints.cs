namespace SpeakingCoach.Api.Endpoints;

/// <summary>
/// IELTS Speaking Part 2 trener: kartochka → 1 daqiqa tayyorgarlik → 2 daqiqa gapirish → baho.
/// Natija Speaking mashqi sifatida saqlanadi (PromptData'da "part2": true) — tarix, XP, kunlik reja,
/// takrorlash kartalari va ravonlik grafigi alohida ishlovsiz ishlaydi.
/// </summary>
public static class Part2Endpoints
{
    private const long BodyBytes = Uploads.MaxRecordingBytes + Uploads.FormOverheadBytes;

    public record CardDto(string Id, string Topic, string[] Points, string Explain, bool Done);

    public static void MapPart2Endpoints(this IEndpointRouteBuilder app)
    {
        // Kartochkalar ro'yxati; kirgan foydalanuvchi uchun — qaysilarini bajargani.
        app.MapGet("/api/part2/cards", async (HttpRequest request, AuthService auth, PracticeStore store, MockSets sets) =>
        {
            var cards = Part2Rules.All(await sets.IeltsSpeakingAsync());
            var done = await auth.GetCurrentUserIdAsync(request) is Guid userId ? await store.Part2DoneAsync(userId) : [];
            return Results.Ok(new
            {
                prepSeconds = Part2Rules.PrepSeconds,
                speakSeconds = Part2Rules.SpeakSeconds,
                cards = cards.Select(c => new CardDto(c.Id, c.Topic, c.Points, c.Explain, done.Contains(c.Id))),
            });
        });

        app.MapPost("/api/part2/submit", async (
            HttpRequest request, AuthService auth, PracticeStore store, AiQuotaService quotas,
            MockSets sets, IPart2Coach coach, ILogger<Program> logger) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest(request.Error("speaking.multipart"));
            if (Uploads.DeclaredTooLarge(request, BodyBytes))
                return Results.Json(request.Error("audio.too_big", Uploads.MaxRecordingBytes / Uploads.Mb), statusCode: StatusCodes.Status413PayloadTooLarge);

            var ct = request.HttpContext.RequestAborted;
            var form = await Uploads.ReadFormAsync(request, ct);
            using var slot = await Uploads.EnterIfLargeAsync(request, ct);

            var card = Part2Rules.All(await sets.IeltsSpeakingAsync()).FirstOrDefault(c => c.Id == form["cardId"].ToString());
            if (card is null) return Results.BadRequest(request.Error("part2.bad_card"));
            var audioFile = form.Files.GetFile("audio");
            if (audioFile is null || audioFile.Length == 0) return Results.BadRequest(request.Error("speaking.no_audio"));
            if (audioFile.Length > Uploads.MaxRecordingBytes)
                return Results.BadRequest(request.Error("audio.too_big", Uploads.MaxRecordingBytes / Uploads.Mb));
            var level = LearnerLevel.Normalize(form["level"].ToString());
            var (seconds, longPauses, pauseSeconds) = FluencyMetrics.Sanitize(
                form["seconds"].ToString(), form["longPauses"].ToString(), form["pauseSeconds"].ToString());

            // Kunlik AI limiti — Gemini'ga murojaatdan OLDIN (oddiy Speaking bilan umumiy).
            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;

            var audio = await Uploads.ReadAsync(audioFile, ct);
            var userId = await auth.GetCurrentUserIdAsync(request);
            var submissionId = Guid.NewGuid();
            try
            {
                var evaluation = await coach.EvaluateAsync(card, audio, audioFile.ContentType, level, RequestTexts.LangOf(request), ct);
                var metrics = FluencyMetrics.Analyze(evaluation.Transcript, seconds, longPauses, pauseSeconds);
                var newCards = 0;
                if (userId is not null)
                {
                    newCards = await store.SavePart2Async(userId.Value, submissionId, card, FluencyMetrics.WithMetrics(evaluation, metrics), evaluation.TopCorrections);
                }
                await quotas.RecordAsync(request, AiKind.Exercise);
                return Results.Ok(new { submissionId, card, evaluation, metrics, saved = userId is not null, newCards });
            }
            catch (Exception ex) when (AiErrors.Handles(ex, request))
            {
                logger.LogError(ex, "Part 2 {Id}: baholanmadi", submissionId);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }).RequireRateLimiting(RateLimits.AiPolicy).WithBodyLimit(BodyBytes);
    }
}
