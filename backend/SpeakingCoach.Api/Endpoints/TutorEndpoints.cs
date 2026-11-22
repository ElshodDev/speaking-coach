using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Endpoints;

public record TutorStartRequest(string? Topic, string? Level);
public record TutorAnswerRequest(string? Section, int Index, int Option);
public record TutorWriteRequest(string? Text);
public record TutorAskRequest(string? Question);

/// <summary>
/// AI ustoz (mini-dars): /start — mavzu bo'yicha dars yaratiladi (bitta mashq limitidan), /answer — javob
/// serverda tekshiriladi (AI'siz), /write — o'z gaplarini baholash, /ask — "boshqacha tushuntir" (darsga ko'pi
/// bilan 5 ta), /finish — natija, xatolar takrorlash kartalariga, faoliyat tarixiga.
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
        static IResult NotFound(HttpRequest r) => Results.NotFound(r.Error("tutor.not_found"));
        static IResult Busy(HttpRequest r) => Results.Json(r.Error("talk.busy"), statusCode: StatusCodes.Status409Conflict);
        static IResult DailyLimit(HttpRequest r, int limit) =>
            Results.Json(r.Error("tutor.daily_limit", limit), statusCode: StatusCodes.Status429TooManyRequests);
        static IResult OffTopic(HttpRequest r) => Results.Json(r.Error("tutor.off_topic"), statusCode: StatusCodes.Status422UnprocessableEntity);

        async Task<(PendingExercise Row, TutorState State)?> LoadAsync(AppDbContext db, Guid id, Guid userId)
        {
            var row = await db.PendingExercises.FirstOrDefaultAsync(p => p.Id == id && p.Type == ActivityType.Lesson);
            if (row is null) return null;
            TutorState? state;
            try
            {
                state = JsonSerializer.Deserialize<TutorState>(row.Payload, Tutor.Json);
            }
            catch (JsonException)
            {
                return null;
            }
            return state is null || state.UserId != userId ? null : (row, state);
        }

        // Sozlama: yoqilganmi va tavsiya etilgan mavzular (kirish talab qilinmaydi — sahifa shunga qarab chiziladi).
        app.MapGet("/api/tutor/config", (FeatureFlags flags) => Results.Ok(new { enabled = flags.AiTutor, topics = Tutor.SuggestedTopics }));

        app.MapPost("/api/tutor/start", async (TutorStartRequest body, HttpRequest request, AuthService auth, AppDbContext db, ITutorAi ai,
            AiQuotaService quotas, FeatureFlags flags, ILogger<Program> logger) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var topic = Tutor.NormalizeTopic(body.Topic);
            if (topic is null) return Results.BadRequest(request.Error("tutor.bad_topic"));

            // Bitta dars = bitta mashq (kunlik AI limiti).
            var limited = await quotas.CheckAsync(request, AiKind.Exercise);
            if (limited is not null) return limited;

            var level = LearnerLevel.Normalize(body.Level ?? user.Level);
            var lang = RequestTexts.LangOf(request);
            TutorLesson lesson;
            try
            {
                lesson = await ai.GenerateAsync(topic, level, lang, request.HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                if (!AiErrors.Handles(ex, request)) throw;
                logger.LogError(ex, "AI ustoz darsi yaratilmadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
            // Mavzu inglizcha o'rganishga oid emas — limit sarflanmaydi.
            if (!lesson.OnTopic) return OffTopic(request);

            var cutoff = DateTime.UtcNow.AddDays(-1);
            await db.PendingExercises.Where(p => p.CreatedAtUtc < cutoff).ExecuteDeleteAsync();

            var state = Tutor.NewState(user.Id, level, lang, lesson);
            var row = new PendingExercise
            {
                Id = Guid.NewGuid(),
                Type = ActivityType.Lesson,
                Payload = JsonSerializer.Serialize(state, Tutor.Json),
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.PendingExercises.Add(row);
            await db.SaveChangesAsync();
            await quotas.RecordAsync(request, AiKind.Exercise);
            return Results.Ok(new { lessonId = row.Id, lesson = Tutor.View(state) });
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapGet("/api/tutor/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db, FeatureFlags flags) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            return await LoadAsync(db, id, user.Id) is { } l
                ? Results.Ok(new { lessonId = id, lesson = Tutor.View(l.State) })
                : NotFound(request);
        });

        app.MapPost("/api/tutor/{id:guid}/answer", async (Guid id, TutorAnswerRequest body, HttpRequest request, AuthService auth, AppDbContext db,
            TalkLocks locks, FeatureFlags flags) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            using var turnLock = locks.TryEnter(id);
            if (turnLock is null) return Busy(request);
            if (await LoadAsync(db, id, user.Id) is not { } l) return NotFound(request);
            var (row, state) = l;

            var result = Tutor.Answer(state, body.Section ?? "", body.Index, body.Option);
            if (result is null) return Results.BadRequest(request.Error("tutor.bad_answer"));
            if (!result.AlreadyAnswered)
            {
                row.Payload = JsonSerializer.Serialize(state, Tutor.Json);
                await db.SaveChangesAsync();
            }
            return Results.Ok(new
            {
                correct = result.Correct,
                correctIndex = result.CorrectIndex,
                explanation = result.Explanation,
                alreadyAnswered = result.AlreadyAnswered,
                practiceDone = Tutor.PracticeDone(state),
                testDone = Tutor.TestDone(state),
            });
        });

        app.MapPost("/api/tutor/{id:guid}/write", async (Guid id, TutorWriteRequest body, HttpRequest request, AuthService auth, AppDbContext db, ITutorAi ai,
            TalkLocks locks, AiDailyCaps caps, AiQuotaOptions limits, AdminOptions admins, FeatureFlags flags, ILogger<Program> logger) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var text = (body.Text ?? "").Trim();
            if (text.Length is < 3 or > Tutor.MaxWritingChars) return Results.BadRequest(request.Error("tutor.bad_text"));
            using var turnLock = locks.TryEnter(id);
            if (turnLock is null) return Busy(request);
            if (await LoadAsync(db, id, user.Id) is not { } l) return NotFound(request);
            var (row, state) = l;
            if (!Tutor.CanWrite(state)) return Results.BadRequest(request.Error("tutor.locked"));

            var capKey = AiDailyCaps.TutorKey(user.Id);
            var day = AiQuotaService.Today(DateTime.UtcNow);
            var charged = !admins.IsAdmin(user);
            if (charged && !caps.TryTake(capKey, day, limits.TutorCalls)) return DailyLimit(request, limits.TutorCalls);
            TutorWriting writing;
            try
            {
                writing = await ai.EvaluateWritingAsync(state, text, request.HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                if (charged) caps.Refund(capKey, day);
                if (!AiErrors.Handles(ex, request)) throw;
                logger.LogError(ex, "AI ustoz yozma topshiriqni baholamadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
            row.Payload = JsonSerializer.Serialize(state with { Writing = writing }, Tutor.Json);
            await db.SaveChangesAsync(CancellationToken.None);
            return Results.Ok(new { writing });
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapPost("/api/tutor/{id:guid}/ask", async (Guid id, TutorAskRequest body, HttpRequest request, AuthService auth, AppDbContext db, ITutorAi ai,
            TalkLocks locks, AiDailyCaps caps, AiQuotaOptions limits, AdminOptions admins, FeatureFlags flags, ILogger<Program> logger) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            var question = (body.Question ?? "").Trim();
            if (question.Length is < 2 or > Tutor.MaxQuestionChars) return Results.BadRequest(request.Error("tutor.bad_text"));
            using var turnLock = locks.TryEnter(id);
            if (turnLock is null) return Busy(request);
            if (await LoadAsync(db, id, user.Id) is not { } l) return NotFound(request);
            var (row, state) = l;
            if (!Tutor.CanAsk(state)) return Results.Json(request.Error("tutor.asks_limit", Tutor.MaxAsksPerLesson), statusCode: StatusCodes.Status429TooManyRequests);

            var capKey = AiDailyCaps.TutorKey(user.Id);
            var day = AiQuotaService.Today(DateTime.UtcNow);
            var charged = !admins.IsAdmin(user);
            if (charged && !caps.TryTake(capKey, day, limits.TutorCalls)) return DailyLimit(request, limits.TutorCalls);
            string? answer;
            try
            {
                answer = Tutor.CleanAnswer(await ai.AskAsync(state, question, request.HttpContext.RequestAborted));
            }
            catch (Exception ex)
            {
                if (charged) caps.Refund(capKey, day);
                if (!AiErrors.Handles(ex, request)) throw;
                logger.LogError(ex, "AI ustoz savolga javob bermadi");
                return Results.Problem(detail: request.T("ai_unavailable"), statusCode: 502);
            }
            // Mavzudan tashqari savol — hisobga olinmaydi (limit va "5 ta savol" sarflanmaydi).
            if (answer is null)
            {
                if (charged) caps.Refund(capKey, day);
                return OffTopic(request);
            }
            state = state with { Asks = state.Asks + 1 };
            row.Payload = JsonSerializer.Serialize(state, Tutor.Json);
            await db.SaveChangesAsync(CancellationToken.None);
            return Results.Ok(new { answer, asksLeft = Tutor.MaxAsksPerLesson - state.Asks });
        }).RequireRateLimiting(RateLimits.AiPolicy);

        app.MapPost("/api/tutor/{id:guid}/finish", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db, ReviewService reviews,
            TalkLocks locks, FeatureFlags flags) =>
        {
            if (!flags.AiTutor) return Disabled(request);
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return LoginFirst(request);
            using var turnLock = locks.TryEnter(id);
            if (turnLock is null) return Busy(request);
            if (await LoadAsync(db, id, user.Id) is not { } l) return NotFound(request);
            var (row, state) = l;
            if (!Tutor.PracticeDone(state) || !Tutor.TestDone(state)) return Results.BadRequest(request.Error("tutor.not_ready"));

            var result = Tutor.Summarize(state);
            var corrections = Tutor.CardCorrections(state, result);
            var activityId = Guid.NewGuid();
            db.Activities.Add(new Activity
            {
                Id = activityId,
                Type = ActivityType.Lesson,
                UserId = user.Id,
                CreatedAtUtc = DateTime.UtcNow,
                PromptData = JsonSerializer.Serialize(new { topic = state.Topic, level = state.Level }, Tutor.Json),
                ResponseData = JsonSerializer.Serialize(new { result, writing = state.Writing }, Tutor.Json),
            });
            db.PendingExercises.Remove(row);
            // Kartalar dars yozuvi bilan bitta SaveChanges'da saqlanadi (AddCardsAsync faqat kontekstga qo'shadi).
            var newCards = await reviews.AddCardsAsync(user.Id, ActivityType.Lesson, ReviewCardFactory.FromCorrections(corrections));
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound(request);
            }
            return Results.Ok(new { id = activityId, topic = state.Topic, result, writing = state.Writing, newCards });
        }).RequireRateLimiting(RateLimits.AiPolicy);
    }
}
