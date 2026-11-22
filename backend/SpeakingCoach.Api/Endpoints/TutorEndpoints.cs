namespace SpeakingCoach.Api.Endpoints;

public record TutorStartRequest(string? Topic, string? Level, Guid? AssignmentId = null, string? Source = null);
public record TutorAnswerRequest(string? Section, int Index, int Option);
public record TutorWriteRequest(string? Text);
public record TutorAskRequest(string? Question);

/// <summary>
/// AI ustoz (mini-dars): /start — mavzu bo'yicha dars yaratiladi (bitta mashq limitidan), /answer — javob
/// serverda tekshiriladi (AI'siz), /write — o'z gaplarini baholash, /ask — "boshqacha tushuntir" (darsga ko'pi
/// bilan 5 ta), /finish — natija, xatolar takrorlash kartalariga, faoliyat tarixiga.
/// Mantiq — Application'dagi TutorService'da (Telegram bot ham o'shani ishlatadi); bu yerda faqat HTTP.
/// Dars holati serverda (PendingExercises, tur — Lesson): to'g'ri javoblar brauzerga javobdan KEYIN ko'rinadi.
/// Yozma topshiriq va savollar — kunlik limitdan (Ai:TutorCallsPerDay); bitta darsda bir vaqtda bitta amal (409).
/// Yoqish/o'chirish: Features__AiTutor. Baza sxemasi o'zgarmaydi.
/// </summary>
public static class TutorEndpoints
{
    public static void MapTutorEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult LoginFirst(HttpRequest r) => Results.Json(r.Error("tutor.login"), statusCode: StatusCodes.Status401Unauthorized);
        static IResult Disabled(HttpRequest r) => Results.Json(r.Error("tutor.disabled"), statusCode: StatusCodes.Status404NotFound);
        static IResult Failed(HttpRequest r, TutorFail f) => Results.Json(r.Error(f.Key, f.Args), statusCode: f.Status);

        // Sozlama: yoqilganmi va tavsiya etilgan mavzular (kirish talab qilinmaydi — sahifa shunga qarab chiziladi).
        app.MapGet("/api/tutor/config", (FeatureFlags flags) => Results.Ok(new { enabled = flags.AiTutor, topics = Tutor.SuggestedTopics }));

        // "Xatolaringiz asosida sizga shu mavzu kerak": AI'siz, limit sarflamaydi.
        app.MapGet("/api/tutor/recommendations", async (HttpRequest request, AuthService auth, TutorService tutor, FeatureFlags flags) =>
        {
            if (!flags.AiTutor) return Results.Ok(new { items = Array.Empty<object>() });
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var items = await tutor.RecommendAsync(user);
            return Results.Ok(new
            {
                items = items.Select(r => new
                {
                    category = r.Category,
                    topic = r.Topic,
                    count = r.Count,
                    last30 = r.Last30,
                    example = r.Example is null ? null : new { original = r.Example.Original, corrected = r.Example.Corrected },
                }),
            });
        });

        // Admin: AI ustoz o'lchovlari (boshlangan/tugagan darslar, mini-test, o'zlashtirish, mavzular, baho). 401 — kirmagan; 403 — admin emas.
        app.MapGet("/api/admin/tutor", async (HttpRequest request, AuthService auth, AdminOptions admins, TutorService tutor, int days = 30) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);
            if (!admins.IsAdmin(user)) return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);
            return Results.Ok(await tutor.AdminStatsAsync(days));
        });

        // AI chaqiradigan amal: AI xatosi — 502 (kunlik limit servis ichida qaytariladi); AiBusy — markaziy middleware (503).
        async Task<IResult> Guarded(HttpRequest request, ILogger<Program> logger, string what, Func<Task<IResult>> run)
        {
            try
            {
                return await run();
            }
            catch (Exception ex)
            {
                if (!AiErrors.Handles(ex, request)) throw;
                logger.LogError(ex, "AI ustoz: {What} bajarilmadi", what);
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
        }

        app.MapPost("/api/tutor/start", async (TutorStartRequest body, HttpRequest request, AuthService auth, TutorService tutor, AiQuotaService quotas,
            FeatureFlags flags, ILogger<Program> logger) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            // Ustoz vazifasi: mavzu vazifadan olinadi (o'quvchi o'zgartira olmaydi), dars sinf bo'yicha umumiy.
            var topic = body.Topic;
            if (body.AssignmentId is { } assignmentId)
            {
                var resolved = await tutor.ResolveAssignmentTopicAsync(assignmentId, user);
                if (!resolved.Ok) return Failed(request, resolved.Fail!);
                topic = resolved.Value;
            }
            if (Tutor.NormalizeTopic(topic) is null) return Results.BadRequest(request.Error("tutor.bad_topic"));

            // Bitta dars = bitta mashq (kunlik AI limiti).
            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;

            return await Guarded(request, logger, "dars yaratish", async () =>
            {
                var r = await tutor.StartAsync(user, topic, body.Level, RequestTexts.LangOf(request), request.HttpContext.RequestAborted, body.AssignmentId is not null, body.Source == "recommended" ? "recommended" : "free");
                if (!r.Ok) return Failed(request, r.Fail!);
                await quotas.RecordAsync(request, AiKind.Exercise);
                return Results.Ok(new { lessonId = r.Value!.LessonId, lesson = Tutor.View(r.Value.State) });
            });
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapGet("/api/tutor/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, TutorService tutor, FeatureFlags flags) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var r = await tutor.GetAsync(id, user);
            return r.Ok ? Results.Ok(new { lessonId = id, lesson = Tutor.View(r.Value!) }) : Failed(request, r.Fail!);
        });

        app.MapPost("/api/tutor/{id:guid}/answer", async (Guid id, TutorAnswerRequest body, HttpRequest request, AuthService auth, TutorService tutor, FeatureFlags flags) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var r = await tutor.AnswerAsync(id, user, body.Section, body.Index, body.Option);
            if (!r.Ok) return Failed(request, r.Fail!);
            var a = r.Value!;
            return Results.Ok(new
            {
                correct = a.Result.Correct,
                correctIndex = a.Result.CorrectIndex,
                explanation = a.Result.Explanation,
                alreadyAnswered = a.Result.AlreadyAnswered,
                practiceDone = a.PracticeDone,
                testDone = a.TestDone,
            });
        });

        app.MapPost("/api/tutor/{id:guid}/write", async (Guid id, TutorWriteRequest body, HttpRequest request, AuthService auth, TutorService tutor,
            FeatureFlags flags, ILogger<Program> logger) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            return await Guarded(request, logger, "yozma topshiriqni baholash", async () =>
            {
                var r = await tutor.WriteAsync(id, user, body.Text, request.HttpContext.RequestAborted);
                return r.Ok ? Results.Ok(new { writing = r.Value }) : Failed(request, r.Fail!);
            });
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapPost("/api/tutor/{id:guid}/ask", async (Guid id, TutorAskRequest body, HttpRequest request, AuthService auth, TutorService tutor,
            FeatureFlags flags, ILogger<Program> logger) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            return await Guarded(request, logger, "savolga javob", async () =>
            {
                var r = await tutor.AskAsync(id, user, body.Question, request.HttpContext.RequestAborted);
                return r.Ok ? Results.Ok(new { answer = r.Value!.Answer, asksLeft = r.Value.AsksLeft }) : Failed(request, r.Fail!);
            });
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapPost("/api/tutor/{id:guid}/finish", async (Guid id, HttpRequest request, AuthService auth, TutorService tutor, FeatureFlags flags) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var r = await tutor.FinishAsync(id, user);
            if (!r.Ok) return Failed(request, r.Fail!);
            var f = r.Value!;
            return Results.Ok(new { id = f.ActivityId, topic = f.State.Topic, result = f.Result, writing = f.State.Writing, newCards = f.NewCards });
        }).RequireRateLimiting(RateLimits.AiPolicy);
    }
}
