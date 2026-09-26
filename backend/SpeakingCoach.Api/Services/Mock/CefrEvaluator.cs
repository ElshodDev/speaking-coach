using System.Text;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services.Mock;

public record PartScore(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("reasoning")] string Reasoning);

public record CefrSpeakingAi(
    [property: JsonPropertyName("answers")] List<AnswerTranscript> Answers,
    [property: JsonPropertyName("part11")] PartScore Part11,
    [property: JsonPropertyName("part12")] PartScore Part12,
    [property: JsonPropertyName("part2")] PartScore Part2,
    [property: JsonPropertyName("part3")] PartScore Part3,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

public record CefrTaskAi(
    [property: JsonPropertyName("task")] PartScore Task,
    [property: JsonPropertyName("organisation")] PartScore Organisation,
    [property: JsonPropertyName("vocabulary")] PartScore Vocabulary,
    [property: JsonPropertyName("grammar")] PartScore Grammar)
{
    public int Total => Task.Score + Organisation.Score + Vocabulary.Score + Grammar.Score;
}

public record CefrWritingAi(
    [property: JsonPropertyName("task11")] CefrTaskAi Task11,
    [property: JsonPropertyName("task12")] CefrTaskAi Task12,
    [property: JsonPropertyName("task2")] CefrTaskAi Task2,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

public record CefrPartResult(string Part, int Score, int Max, string Reasoning);
public record CefrAnswer(int Index, string Part, string Question, string Transcript, int Seconds);

public record CefrSpeakingResult(
    string Exam, string Module, string SetId, int Overall, int Max, string Level,
    List<CefrPartResult> Parts, List<CefrAnswer> Answers, List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

public record CefrTaskResult(string Task, int Raw, int Points, decimal MaxPoints, int Words, int MinWords, int MaxWords, string Text, CefrTaskAi Criteria);

public record CefrWritingResult(
    string Exam, string Module, string SetId, int Overall, int Max, string Level, int SecondsUsed,
    List<CefrTaskResult> Tasks, List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

public interface ICefrEvaluator
{
    Task<CefrSpeakingResult> EvaluateSpeakingAsync(CefrSpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang, CancellationToken ct = default);
    Task<CefrWritingResult> EvaluateWritingAsync(CefrWritingSet set, string t11, string t12, string t2, int secondsUsed, string lang, CancellationToken ct = default);
}

/// <summary>
/// CEFR (Multilevel) mock'ni Gemini bilan baholash. AI qism/mezon ballarini
/// beradi, 0–75 ball va darajani server hisoblaydi (CefrScale).
/// </summary>
public class GeminiCefrEvaluator(GeminiClient gemini) : ICefrEvaluator
{
    private const string Examiner = """
        You are a strict but fair examiner for Uzbekistan's national CEFR-based multilevel English test.
        Judge ONLY what is in the response, cite the candidate's exact words, and do not inflate scores.
        """;

    public static string BuildSpeakingPrompt(CefrSpeakingSet set, string lang)
    {
        var sb = new StringBuilder(Examiner);
        sb.AppendLine("This is a full multilevel Speaking mock. Below, each question is followed by the candidate's recorded answer (audio). Some may be missing.");
        sb.AppendLine("Part 1.1: three short personal questions. Part 1.2: two pictures and three questions about them.");
        sb.AppendLine($"Pictures: {string.Join("; ", set.Pictures.Select(p => p.Caption))}.");
        sb.AppendLine($"Part 2: a long turn (up to 2 minutes) on a topic with guiding questions: {string.Join(" ", set.Part2Questions)}");
        sb.AppendLine($"Part 3: a balanced argument (up to 2 minutes) about the statement \"{set.Part3Statement}\"; the candidate saw these points FOR: {string.Join("; ", set.For)}; AGAINST: {string.Join("; ", set.Against)}.");
        sb.AppendLine("First transcribe every answer exactly (keep hesitations). Then score each part holistically, considering fluency and coherence, vocabulary, grammar and task completion:");
        sb.AppendLine("part11 0-5, part12 0-5, part2 0-5, part3 0-6. A missing or very short answer must get a low score.");
        sb.AppendLine($"Write every \"reasoning\", \"strengths\", \"nextSteps\" item and correction \"explanation\" in {GeminiIeltsEvaluator.FeedbackLanguage(lang)}.");
        sb.AppendLine("""
            Return ONLY valid JSON, no markdown fences:
            {"answers": [{"index": <int>, "transcript": "<exact transcription or empty string>"}],
             "part11": {"score": <0-5>, "reasoning": "..."}, "part12": {"score": <0-5>, "reasoning": "..."},
             "part2": {"score": <0-5>, "reasoning": "..."}, "part3": {"score": <0-6>, "reasoning": "..."},
             "topCorrections": [{"original": "...", "corrected": "...", "explanation": "..."}],
             "strengths": "...", "nextSteps": ["...", "...", "..."]}
            Give at most 5 topCorrections.
            """);
        return sb.ToString();
    }

    public static string BuildWritingPrompt(CefrWritingSet set, string t11, string t12, string t2, string lang)
    {
        var sb = new StringBuilder(Examiner);
        sb.AppendLine("This is a full multilevel Writing mock. Assess each of the three tasks separately with four criteria, each 0-5:");
        sb.AppendLine("task (task achievement: all points covered, right register and format), organisation (coherence, paragraphs, linking), vocabulary (range and accuracy), grammar (range and accuracy).");
        sb.AppendLine("Task 1.1 is an INFORMAL letter to a friend; task 1.2 is a FORMAL letter; task 2 is an online discussion post (opinion with reasons and examples).");
        sb.AppendLine("A response far outside the word range, off-topic, or in the wrong register must lose marks in \"task\". An empty response gets 0 for every criterion.");
        sb.AppendLine($"Write every \"reasoning\", \"strengths\", \"nextSteps\" item and correction \"explanation\" in {GeminiIeltsEvaluator.FeedbackLanguage(lang)}.");
        sb.AppendLine();
        sb.AppendLine("=== SITUATION ===");
        sb.AppendLine(set.Role);
        sb.AppendLine(set.Email);
        void Task(string name, string prompt, (int Min, int Max) range, string text)
        {
            sb.AppendLine($"=== TASK {name} ({range.Min}-{range.Max} words) ===");
            sb.AppendLine(prompt);
            sb.AppendLine($"--- ANSWER ({IeltsBand.CountWords(text)} words) ---");
            sb.AppendLine(string.IsNullOrWhiteSpace(text) ? "(no answer)" : text);
            sb.AppendLine();
        }
        Task("1.1", set.Task11, CefrBank.Words11, t11);
        Task("1.2", set.Task12, CefrBank.Words12, t12);
        Task("2", set.Task2, CefrBank.Words2, t2);
        sb.AppendLine("""
            Return ONLY valid JSON, no markdown fences:
            {"task11": {"task": {"score": <0-5>, "reasoning": "..."}, "organisation": {...}, "vocabulary": {...}, "grammar": {...}},
             "task12": {same shape}, "task2": {same shape},
             "topCorrections": [{"original": "...", "corrected": "...", "explanation": "..."}],
             "strengths": "...", "nextSteps": ["...", "...", "..."]}
            Give at most 5 topCorrections.
            """);
        return sb.ToString();
    }

    public static void EnsureScores(params (string Name, PartScore S, int Max)[] scores)
    {
        foreach (var (name, s, max) in scores)
        {
            if (s.Score < 0 || s.Score > max) throw new InvalidOperationException($"Gemini '{name}' uchun 0-{max} dan tashqari ball: {s.Score}");
            if (string.IsNullOrWhiteSpace(s.Reasoning)) throw new InvalidOperationException($"Gemini '{name}' uchun izoh bermadi");
        }
    }

    public async Task<CefrSpeakingResult> EvaluateSpeakingAsync(CefrSpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang, CancellationToken ct = default)
    {
        var questions = set.Questions();
        var parts = new List<object> { new { text = BuildSpeakingPrompt(set, lang) } };
        for (var i = 0; i < questions.Count; i++)
        {
            parts.Add(new { text = $"Question {i} (Part {questions[i].Part}): {questions[i].Text}" });
            var a = answers.FirstOrDefault(x => x.Index == i);
            parts.Add(a?.Audio is { Length: > 0 } audio
                ? (object)new { inline_data = new { mime_type = (a.MimeType ?? "audio/webm").Split(';')[0], data = Convert.ToBase64String(audio) } }
                : new { text = "(no answer recorded)" });
        }
        var body = new
        {
            contents = new[] { new { parts = parts.ToArray() } },
            generationConfig = new { temperature = 0.2, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var ai = GeminiClient.DeserializeStrict<CefrSpeakingAi>(await GeminiClient.ExtractTextAsync(response, ct));
        return BuildSpeakingResult(set, answers, ai);
    }

    public static CefrSpeakingResult BuildSpeakingResult(CefrSpeakingSet set, IReadOnlyList<SpokenAnswer> answers, CefrSpeakingAi ai)
    {
        EnsureScores(("part11", ai.Part11, 5), ("part12", ai.Part12, 5), ("part2", ai.Part2, 5), ("part3", ai.Part3, 6));
        var scores = new[] { ai.Part11.Score, ai.Part12.Score, ai.Part2.Score, ai.Part3.Score };
        var total = CefrScale.SpeakingScore(scores);
        var questions = set.Questions();
        var list = questions.Select((q, i) =>
        {
            var spoken = answers.FirstOrDefault(x => x.Index == i);
            var transcript = spoken?.Audio is { Length: > 0 } ? ai.Answers.FirstOrDefault(t => t.Index == i)?.Transcript ?? "" : "";
            return new CefrAnswer(i, q.Part, q.Text, transcript, spoken?.Seconds ?? 0);
        }).ToList();
        return new CefrSpeakingResult("cefr", "speaking", set.Id, total, CefrScale.Max, CefrScale.Level(total),
            [
                new("1.1", ai.Part11.Score, 5, ai.Part11.Reasoning),
                new("1.2", ai.Part12.Score, 5, ai.Part12.Reasoning),
                new("2", ai.Part2.Score, 5, ai.Part2.Reasoning),
                new("3", ai.Part3.Score, 6, ai.Part3.Reasoning),
            ],
            list, ai.TopCorrections.Take(5).ToList(), ai.Strengths, ai.NextSteps.Take(3).ToList());
    }

    public async Task<CefrWritingResult> EvaluateWritingAsync(CefrWritingSet set, string t11, string t12, string t2, int secondsUsed, string lang, CancellationToken ct = default)
    {
        var body = new
        {
            contents = new[] { new { parts = new object[] { new { text = BuildWritingPrompt(set, t11, t12, t2, lang) } } } },
            generationConfig = new { temperature = 0.2, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var ai = GeminiClient.DeserializeStrict<CefrWritingAi>(await GeminiClient.ExtractTextAsync(response, ct));
        return BuildWritingResult(set, t11, t12, t2, secondsUsed, ai);
    }

    public static CefrWritingResult BuildWritingResult(CefrWritingSet set, string t11, string t12, string t2, int secondsUsed, CefrWritingAi ai)
    {
        foreach (var (name, t) in new[] { ("1.1", ai.Task11), ("1.2", ai.Task12), ("2", ai.Task2) })
        {
            EnsureScores(($"{name}.task", t.Task, 5), ($"{name}.organisation", t.Organisation, 5),
                ($"{name}.vocabulary", t.Vocabulary, 5), ($"{name}.grammar", t.Grammar, 5));
        }
        var raws = new[] { ai.Task11.Total, ai.Task12.Total, ai.Task2.Total };
        var total = CefrScale.WritingScore(raws);
        CefrTaskResult R(int i, string name, (int Min, int Max) range, string text, CefrTaskAi c) =>
            new(name, c.Total, (int)Math.Round(c.Total / 20m * CefrScale.WritingWeights[i], MidpointRounding.AwayFromZero),
                CefrScale.WritingWeights[i], IeltsBand.CountWords(text), range.Min, range.Max, text, c);
        return new CefrWritingResult("cefr", "writing", set.Id, total, CefrScale.Max, CefrScale.Level(total), secondsUsed,
            [R(0, "1.1", CefrBank.Words11, t11, ai.Task11), R(1, "1.2", CefrBank.Words12, t12, ai.Task12), R(2, "2", CefrBank.Words2, t2, ai.Task2)],
            ai.TopCorrections.Take(5).ToList(), ai.Strengths, ai.NextSteps.Take(3).ToList());
    }
}
