using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Application.Abstractions;
namespace SpeakingCoach.Application;

/// <summary>Muvaffaqiyatsizlik: xabar kaliti (Texts), HTTP'ga o'xshash holat kodi va parametrlari.</summary>
public record TutorFail(string Key, int Status, params object?[] Args);

public class TutorOutcome<T>
{
    public T? Value { get; private init; }
    public TutorFail? Fail { get; private init; }
    public bool Ok => Fail is null;

    public static TutorOutcome<T> Success(T value) => new() { Value = value };
    public static TutorOutcome<T> Failure(string key, int status, params object?[] args) => new() { Fail = new TutorFail(key, status, args) };
}

public record TutorStarted(Guid LessonId, TutorState State);
public record TutorAnswered(TutorAnswerResult Result, bool PracticeDone, bool TestDone);
public record TutorAsked(string Answer, int AsksLeft);
public record TutorFinished(Guid ActivityId, TutorState State, TutorResult Result, int NewCards);

/// <summary>
/// Ustoz vazifasi bo'yicha yaratilgan darslar xotirada (mavzu+daraja+til bo'yicha): butun sinf bir xil dars oladi
/// (Gemini bitta marta chaqiriladi, o'qituvchi savollar bo'yicha umumiy xatolarni solishtira oladi).
/// Server qayta ishga tushsa bo'shaydi — muhim emas. Xato natija saqlanmaydi.
/// </summary>
public class TutorLessonCache
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);
    public const int MaxEntries = 100;

    private sealed record Entry(Lazy<Task<TutorLesson>> Task, DateTime ExpiresUtc);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Entry> _items = new();

    public static string Key(string topic, string level, string lang) => $"{topic.Trim().ToLowerInvariant()}|{level}|{lang}";

    public int Count => _items.Count;

    public async Task<TutorLesson> GetOrCreateAsync(string key, Func<Task<TutorLesson>> create, DateTime nowUtc)
    {
        if (_items.Count >= MaxEntries)
            foreach (var old in _items.Where(kv => kv.Value.ExpiresUtc <= nowUtc).Select(kv => kv.Key).ToList()) _items.TryRemove(old, out _);
        if (_items.Count >= MaxEntries) _items.TryRemove(_items.OrderBy(kv => kv.Value.ExpiresUtc).First().Key, out _);

        var entry = _items.AddOrUpdate(
            key,
            _ => new Entry(new Lazy<Task<TutorLesson>>(create), nowUtc + Lifetime),
            (_, cur) => cur.ExpiresUtc > nowUtc ? cur : new Entry(new Lazy<Task<TutorLesson>>(create), nowUtc + Lifetime));
        try
        {
            return await entry.Task.Value;
        }
        catch
        {
            // Muvaffaqiyatsiz urinish keshda qolmasin: keyingisi qayta urinadi.
            _items.TryRemove(new KeyValuePair<string, Entry>(key, entry));
            throw;
        }
    }
}

/// <summary>
/// AI ustoz amallari — veb (/api/tutor/*) va Telegram bot uchun BITTA yadro: dars holati PendingExercises'da
/// (tur — Lesson), natija Activities'da. Kvota tekshiruvi (mashq limiti) chaqiruvchida; kunlik "savol/yozma" limiti
/// (Ai:TutorCallsPerDay) shu yerda. AI xatolari yuqoriga uzatiladi (kunlik limit qaytariladi).
/// </summary>
public class TutorService(IAppDbContext db, ITutorAi ai, AiDailyCaps caps, AiQuotaOptions limits, AdminOptions admins, TalkLocks locks, ReviewService reviews, TutorLessonCache cache)
{
    private static TutorOutcome<T> Fail<T>(string key, int status, params object?[] args) => TutorOutcome<T>.Failure(key, status, args);

    /// <summary>
    /// Ustoz vazifasining mavzusi: faqat guruh a'zosi (yoki guruh egasi) uchun va faqat "AI dars" turidagi vazifa.
    /// </summary>
    public async Task<TutorOutcome<string>> ResolveAssignmentTopicAsync(Guid assignmentId, User user)
    {
        var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == assignmentId);
        var topic = GroupLogic.LessonTopic(a?.Kind);
        if (a is null || topic is null) return Fail<string>("tutor.bad_assignment", 404);
        var allowed = await db.GroupMembers.AnyAsync(m => m.GroupId == a.GroupId && m.UserId == user.Id)
            || await db.Groups.AnyAsync(g => g.Id == a.GroupId && g.TeacherId == user.Id);
        return allowed ? TutorOutcome<string>.Success(topic) : Fail<string>("tutor.bad_assignment", 404);
    }

    /// <param name="assigned">Ustoz vazifasidan: dars umumiy keshdan olinadi va mavzu aynan vazifadagidek saqlanadi (vazifa bajarilganini aniqlash uchun).</param>
    public async Task<TutorOutcome<TutorStarted>> StartAsync(User user, string? rawTopic, string? level, string lang, CancellationToken ct, bool assigned = false)
    {
        var topic = Tutor.NormalizeTopic(rawTopic);
        if (topic is null) return Fail<TutorStarted>("tutor.bad_topic", 400);
        var lvl = LearnerLevel.Normalize(level ?? user.Level);
        TutorLesson lesson;
        if (assigned)
        {
            // Umumiy so'rov: birinchi o'quvchining bekor qilishi boshqalarni to'xtatmasin.
            lesson = await cache.GetOrCreateAsync(TutorLessonCache.Key(topic, lvl, lang),
                () => ai.GenerateAsync(topic, lvl, lang, CancellationToken.None), DateTime.UtcNow);
            if (lesson.OnTopic) lesson = lesson with { Topic = topic };
        }
        else
        {
            lesson = await ai.GenerateAsync(topic, lvl, lang, ct);
        }
        // Mavzu inglizcha o'rganishga oid emas — chaqiruvchi limitni sarflamaydi.
        if (!lesson.OnTopic) return Fail<TutorStarted>("tutor.off_topic", 422);

        var cutoff = DateTime.UtcNow.AddDays(-1);
        await db.PendingExercises.Where(p => p.CreatedAtUtc < cutoff).ExecuteDeleteAsync(CancellationToken.None);

        var state = Tutor.NewState(user.Id, lvl, lang, lesson);
        var row = new PendingExercise
        {
            Id = Guid.NewGuid(),
            Type = ActivityType.Lesson,
            Payload = JsonSerializer.Serialize(state, Tutor.Json),
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.PendingExercises.Add(row);
        await db.SaveChangesAsync(CancellationToken.None);
        return TutorOutcome<TutorStarted>.Success(new TutorStarted(row.Id, state));
    }

    public async Task<(PendingExercise Row, TutorState State)?> LoadAsync(Guid id, Guid userId)
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

    public async Task<TutorOutcome<TutorState>> GetAsync(Guid id, User user) =>
        await LoadAsync(id, user.Id) is { } l ? TutorOutcome<TutorState>.Success(l.State) : Fail<TutorState>("tutor.not_found", 404);

    public async Task<TutorOutcome<TutorAnswered>> AnswerAsync(Guid id, User user, string? section, int index, int option)
    {
        using var turnLock = locks.TryEnter(id);
        if (turnLock is null) return Fail<TutorAnswered>("talk.busy", 409);
        if (await LoadAsync(id, user.Id) is not { } l) return Fail<TutorAnswered>("tutor.not_found", 404);
        var (row, state) = l;
        var result = Tutor.Answer(state, section ?? "", index, option);
        if (result is null) return Fail<TutorAnswered>("tutor.bad_answer", 400);
        if (!result.AlreadyAnswered)
        {
            row.Payload = JsonSerializer.Serialize(state, Tutor.Json);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        return TutorOutcome<TutorAnswered>.Success(new TutorAnswered(result, Tutor.PracticeDone(state), Tutor.TestDone(state)));
    }

    /// <summary>Kunlik savol/yozma limitidan bitta joy (admin — cheksiz). null — joy bor; aks holda rad etish.</summary>
    private TutorFail? TakeCall(User user, out Action refund)
    {
        var key = AiDailyCaps.TutorKey(user.Id);
        var day = AiQuotaService.Today(DateTime.UtcNow);
        var charged = !admins.IsAdmin(user);
        if (charged && !caps.TryTake(key, day, limits.TutorCalls))
        {
            refund = () => { };
            return new TutorFail("tutor.daily_limit", 429, limits.TutorCalls);
        }
        refund = () =>
        {
            if (charged) caps.Refund(key, day);
        };
        return null;
    }

    public async Task<TutorOutcome<TutorWriting>> WriteAsync(Guid id, User user, string? rawText, CancellationToken ct)
    {
        var text = (rawText ?? "").Trim();
        if (text.Length is < 3 or > Tutor.MaxWritingChars) return Fail<TutorWriting>("tutor.bad_text", 400);
        using var turnLock = locks.TryEnter(id);
        if (turnLock is null) return Fail<TutorWriting>("talk.busy", 409);
        if (await LoadAsync(id, user.Id) is not { } l) return Fail<TutorWriting>("tutor.not_found", 404);
        var (row, state) = l;
        if (!Tutor.CanWrite(state)) return Fail<TutorWriting>("tutor.locked", 400);

        if (TakeCall(user, out var refund) is { } denied) return TutorOutcome<TutorWriting>.Failure(denied.Key, denied.Status, denied.Args);
        TutorWriting writing;
        try
        {
            writing = await ai.EvaluateWritingAsync(state, text, ct);
        }
        catch
        {
            refund();
            throw;
        }
        row.Payload = JsonSerializer.Serialize(state with { Writing = writing }, Tutor.Json);
        await db.SaveChangesAsync(CancellationToken.None);
        return TutorOutcome<TutorWriting>.Success(writing);
    }

    public async Task<TutorOutcome<TutorAsked>> AskAsync(Guid id, User user, string? rawQuestion, CancellationToken ct)
    {
        var question = (rawQuestion ?? "").Trim();
        if (question.Length is < 2 or > Tutor.MaxQuestionChars) return Fail<TutorAsked>("tutor.bad_text", 400);
        using var turnLock = locks.TryEnter(id);
        if (turnLock is null) return Fail<TutorAsked>("talk.busy", 409);
        if (await LoadAsync(id, user.Id) is not { } l) return Fail<TutorAsked>("tutor.not_found", 404);
        var (row, state) = l;
        if (!Tutor.CanAsk(state)) return Fail<TutorAsked>("tutor.asks_limit", 429, Tutor.MaxAsksPerLesson);

        if (TakeCall(user, out var refund) is { } denied) return TutorOutcome<TutorAsked>.Failure(denied.Key, denied.Status, denied.Args);
        string? answer;
        try
        {
            answer = Tutor.CleanAnswer(await ai.AskAsync(state, question, ct));
        }
        catch
        {
            refund();
            throw;
        }
        // Mavzudan tashqari savol — hisobga olinmaydi (limit va "5 ta savol" sarflanmaydi).
        if (answer is null)
        {
            refund();
            return Fail<TutorAsked>("tutor.off_topic", 422);
        }
        state = state with { Asks = state.Asks + 1 };
        row.Payload = JsonSerializer.Serialize(state, Tutor.Json);
        await db.SaveChangesAsync(CancellationToken.None);
        return TutorOutcome<TutorAsked>.Success(new TutorAsked(answer, Tutor.MaxAsksPerLesson - state.Asks));
    }

    public async Task<TutorOutcome<TutorFinished>> FinishAsync(Guid id, User user)
    {
        using var turnLock = locks.TryEnter(id);
        if (turnLock is null) return Fail<TutorFinished>("talk.busy", 409);
        if (await LoadAsync(id, user.Id) is not { } l) return Fail<TutorFinished>("tutor.not_found", 404);
        var (row, state) = l;
        if (!Tutor.PracticeDone(state) || !Tutor.TestDone(state)) return Fail<TutorFinished>("tutor.not_ready", 400);

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
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<TutorFinished>("tutor.not_found", 404);
        }
        return TutorOutcome<TutorFinished>.Success(new TutorFinished(activityId, state, result, newCards));
    }
}
