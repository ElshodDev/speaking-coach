using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>Soxta AI: standart — tayyor natija; test <see cref="SpeakingFailure"/> / <see cref="WritingFailure"/> bilan xato berishi mumkin.</summary>
public class FakeAi : ISpeakingEvaluationService, IWritingEvaluationService, ITalkAi
{
    public Exception? TalkFailure { get; set; }
    /// <summary>null — foydalanuvchi yozgan matn (yoki "heard audio"); "" — hech narsa eshitilmadi.</summary>
    public string? TalkHeard { get; set; }

    public Exception? SpeakingFailure { get; set; }
    public Exception? WritingFailure { get; set; }
    public int SpeakingCalls { get; private set; }
    public int WritingCalls { get; private set; }

    /// <summary>ITelegramApi o'rniga: yuborilgan xabarlarni yozib boradi.</summary>
    public RecordingTelegram Telegram { get; } = RecordingTelegram.Create();

    public Task<SpeakingEvaluationResult> EvaluateAsync(string topic, byte[] audioBytes, string audioMimeType, string level, CancellationToken ct = default)
    {
        SpeakingCalls++;
        if (SpeakingFailure is { } ex) throw ex;
        return Task.FromResult(new SpeakingEvaluationResult(
            "I like to travel to new places with my family every summer",
            new ScoreWithReasoning(7, "Smooth"), new ScoreWithReasoning(6, "Some slips"), new ScoreWithReasoning(7, "Good range"),
            [new CorrectionItem("I goed there", "I went there", "past simple of go")],
            "Well done!", "Use more linking words"));
    }

    public Task<TalkTurnResult> TurnAsync(TalkScenario s, string level, IReadOnlyList<TalkLine> history, string lang, byte[]? audio, string? mime, string? typed, CancellationToken ct)
    {
        if (TalkFailure is { } ex) throw ex;
        return Task.FromResult(new TalkTurnResult(TalkHeard ?? typed ?? "I would like a coffee", "Sure, anything else?", new CorrectionItem("I want coffee", "I would like a coffee", "more polite")));
    }

    public Task<TalkSummary> SummarizeAsync(TalkScenario s, string level, IReadOnlyList<TalkLine> lines, string lang, CancellationToken ct) =>
        Task.FromResult(new TalkSummary("Nice chat", "Clear answers", [new CorrectionItem("I goed", "I went", "go → went")], ["Could I have..."], "Use polite requests"));

    public Task<WritingEvaluationResult> EvaluateAsync(string topic, string essayText, string level, CancellationToken ct = default)
    {
        WritingCalls++;
        if (WritingFailure is { } ex) throw ex;
        return Task.FromResult(new WritingEvaluationResult(
            new ScoreWithReasoning(7, "On topic"), new ScoreWithReasoning(6, "Clear"), new ScoreWithReasoning(6, "Mostly accurate"), new ScoreWithReasoning(7, "Varied"),
            [new CorrectionItem("He go to school", "He goes to school", "third person -s")],
            "Nice essay", "Vary your sentences"));
    }
}

/// <summary>ITelegramApi uchun yozib boruvchi vakil (DispatchProxy): faqat SendMessageAsync yozib olinadi, qolganlari — bo'sh natija.</summary>
public class RecordingTelegram : DispatchProxy
{
    public List<(long ChatId, string Html)> Sent { get; } = [];

    public static RecordingTelegram Create() => (RecordingTelegram)(object)DispatchProxy.Create<ITelegramApi, RecordingTelegram>();

    public ITelegramApi Api => (ITelegramApi)(object)this;

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == nameof(ITelegramApi.SendMessageAsync))
        {
            lock (Sent) Sent.Add(((long)args![0]!, (string)args[1]!));
            return Task.FromResult<long?>(1);
        }
        return method.ReturnType == typeof(Task) ? Task.CompletedTask : null;
    }
}

/// <summary>MockRowsQuery (Postgres jsonb) o'rniga: shu ma'lumotni xotirada JSON'dan o'qiydi.</summary>
public class InMemoryMockRows(AppDbContext db) : IMockRows
{
    public async Task<List<MockQueries.MockRow>> ForUserAsync(Guid userId, int take, CancellationToken ct = default)
    {
        var rows = await db.Activities.Where(a => a.UserId == userId && a.Type == ActivityType.MockExam).ToListAsync(ct);
        return rows.OrderByDescending(a => a.CreatedAtUtc).Take(take).Select(a =>
        {
            var p = MockQueries.ParsePrompt(a.PromptData);
            decimal? overall = null;
            using var doc = JsonDocument.Parse(a.ResponseData);
            if (doc.RootElement.TryGetProperty("overall", out var o) && o.ValueKind == JsonValueKind.Number) overall = o.GetDecimal();
            return new MockQueries.MockRow { Id = a.Id, CreatedAtUtc = a.CreatedAtUtc, Exam = p.Exam, Module = p.Module, SetId = p.SetId, Variant = p.Variant, SessionId = p.SessionId, Overall = overall };
        }).ToList();
    }
}

/// <summary>PracticeHistory (Postgres jsonb) o'rniga: xotirada.</summary>
public class InMemoryPracticeHistory(AppDbContext db) : IPracticeHistory
{
    public async Task<List<string>> DoneTitlesAsync(Guid userId, ActivityType type)
    {
        var rows = await db.Activities.Where(a => a.UserId == userId && a.Type == type).ToListAsync();
        return rows.OrderByDescending(a => a.CreatedAtUtc).Take(300)
            .Select(a => { using var d = JsonDocument.Parse(a.PromptData); return d.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString()! : null; })
            .OfType<string>().ToList();
    }

    public async Task<List<string>> Part2PromptsAsync(Guid userId)
    {
        var rows = await db.Activities.Where(a => a.UserId == userId && a.Type == ActivityType.Speaking).ToListAsync();
        return rows.Where(a => a.PromptData.Contains("\"part2\"")).OrderByDescending(a => a.CreatedAtUtc).Take(300).Select(a => a.PromptData).ToList();
    }

    public async Task<List<FluencyRow>> FluencyRowsAsync(Guid userId, int take)
    {
        ActivityType[] types = [ActivityType.Speaking, ActivityType.MockExam, ActivityType.Conversation];
        var rows = await db.Activities.Where(a => a.UserId == userId && types.Contains(a.Type)).ToListAsync();
        return rows.Where(a => a.ResponseData.Contains("\"metrics\"")).OrderByDescending(a => a.CreatedAtUtc).Take(take)
            .Select(a => new FluencyRow(a.CreatedAtUtc, a.Type, a.ResponseData)).ToList();
    }
}
