using System.Text;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services.Mock;

public record PartScore(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("reasoning")] string Reasoning);

public record CefrPartComments(
    [property: JsonPropertyName("part11")] string Part11,
    [property: JsonPropertyName("part12")] string Part12,
    [property: JsonPropertyName("part2")] string Part2,
    [property: JsonPropertyName("part3")] string Part3);

/// <summary>Speaking: rasmiy tartibda — butun nutq uchun bitta yaxlit baho (0–36), qismlar bo'yicha izoh.</summary>
public record CefrSpeakingAi(
    [property: JsonPropertyName("answers")] List<AnswerTranscript> Answers,
    [property: JsonPropertyName("overall")] PartScore Overall,
    [property: JsonPropertyName("parts")] CefrPartComments Parts,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

/// <summary>Bitta Writing topshirig'i: 4 mezon. 1-topshiriq: har biri 0–3 (jami 12), 2-topshiriq: 0–6 (jami 24).</summary>
public record CefrTaskAi(
    [property: JsonPropertyName("task")] PartScore Task,
    [property: JsonPropertyName("organisation")] PartScore Organisation,
    [property: JsonPropertyName("vocabulary")] PartScore Vocabulary,
    [property: JsonPropertyName("grammar")] PartScore Grammar)
{
    public int Total => Task.Score + Organisation.Score + Vocabulary.Score + Grammar.Score;
}

public record CefrWritingAi(
    [property: JsonPropertyName("task1")] CefrTaskAi Task1,
    [property: JsonPropertyName("task2")] CefrTaskAi Task2,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

public record CefrPartResult(string Part, string Comment);
public record CefrAnswer(int Index, string Part, string Question, string Transcript, int Seconds);

public record CefrSpeakingResult(
    string Exam, string Module, string SetId, int Overall, int Max, string Level, int Raw, int RawMax, string Reasoning,
    List<CefrPartResult> Parts, List<CefrAnswer> Answers, List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

public record CefrText(string Task, int Words, int MinWords, int MaxWords, string Text);
public record CefrTaskResult(string Task, int Raw, int Max, CefrTaskAi Criteria);

public record CefrWritingResult(
    string Exam, string Module, string SetId, int Overall, int Max, string Level, int Raw, int RawMax, int SecondsUsed,
    List<CefrTaskResult> Tasks, List<CefrText> Texts, List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

public interface ICefrEvaluator
{
    Task<CefrSpeakingResult> EvaluateSpeakingAsync(CefrSpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang, CancellationToken ct = default);
    Task<CefrWritingResult> EvaluateWritingAsync(CefrWritingSet set, string t11, string t12, string t2, int secondsUsed, string lang, CancellationToken ct = default);
}

/// <summary>
/// CEFR (Multilevel) mock'ni Gemini bilan baholash — rasmiy tartibda: Writing
/// 12 + 24 xom ball, Speaking — yaxlit 0–36; 0–75 ball va darajani server
/// rasmiy jadval bilan hisoblaydi (CefrScale).
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
        sb.AppendLine("First transcribe every answer exactly (keep hesitations).");
        sb.AppendLine($"Then give ONE holistic score for the whole speaking test on a 0-{CefrScale.RawMax} scale, as the official multilevel assessment does,");
        sb.AppendLine("considering fluency and coherence, vocabulary, grammar, pronunciation and task completion across all parts.");
        sb.AppendLine("Guidance from the official conversion table: 15-20 = B1, 21-28 = B2, 29-36 = C1, 14 or less = below B1; missing or very short answers must lower the score.");
        sb.AppendLine("Also write one short comment per part.");
        sb.AppendLine($"Write every \"reasoning\", part comment, \"strengths\", \"nextSteps\" item and correction \"explanation\" in {GeminiIeltsEvaluator.FeedbackLanguage(lang)}.");
        sb.AppendLine("""
            Return ONLY valid JSON, no markdown fences:
            {"answers": [{"index": <int>, "transcript": "<exact transcription or empty string>"}],
             "overall": {"score": <0-36>, "reasoning": "<evidence with quotes>"},
             "parts": {"part11": "...", "part12": "...", "part2": "...", "part3": "..."},
             "topCorrections": [{"original": "...", "corrected": "...", "explanation": "..."}],
             "strengths": "...", "nextSteps": ["...", "...", "..."]}
            Give at most 5 topCorrections.
            """);
        return sb.ToString();
    }

    public static string BuildWritingPrompt(CefrWritingSet set, string t11, string t12, string t2, string lang)
    {
        var sb = new StringBuilder(Examiner);
        sb.AppendLine("This is a full multilevel Writing mock with two tasks, assessed as in the official scheme:");
        sb.AppendLine($"TASK 1 (worth {CefrScale.WritingTask1Max} points) consists of two letters based on the same situation: 1.1 an INFORMAL letter to a friend and 1.2 a FORMAL letter. Assess them TOGETHER.");
        sb.AppendLine($"TASK 2 (worth {CefrScale.WritingTask2Max} points) is an online discussion post giving an opinion with reasons and examples.");
        sb.AppendLine("Use four criteria for each task: task (all points covered, correct register, format and length), organisation (coherence, paragraphs, linking), vocabulary (range and accuracy), grammar (range and accuracy).");
        sb.AppendLine("For Task 1 each criterion is scored 0-3; for Task 2 each criterion is scored 0-6.");
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
            {"task1": {"task": {"score": <0-3>, "reasoning": "..."}, "organisation": {...}, "vocabulary": {...}, "grammar": {...}},
             "task2": {"task": {"score": <0-6>, "reasoning": "..."}, "organisation": {...}, "vocabulary": {...}, "grammar": {...}},
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
        EnsureScores(("overall", ai.Overall, CefrScale.RawMax));
        var total = CefrScale.Convert(ai.Overall.Score);
        var list = set.Questions().Select((q, i) =>
        {
            var spoken = answers.FirstOrDefault(x => x.Index == i);
            var transcript = spoken?.Audio is { Length: > 0 } ? ai.Answers.FirstOrDefault(t => t.Index == i)?.Transcript ?? "" : "";
            return new CefrAnswer(i, q.Part, q.Text, transcript, spoken?.Seconds ?? 0);
        }).ToList();
        return new CefrSpeakingResult("cefr", "speaking", set.Id, total, CefrScale.Max, CefrScale.Level(total), ai.Overall.Score, CefrScale.RawMax, ai.Overall.Reasoning,
            [new("1.1", ai.Parts.Part11), new("1.2", ai.Parts.Part12), new("2", ai.Parts.Part2), new("3", ai.Parts.Part3)],
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
        void Check(string name, CefrTaskAi t, int max) => EnsureScores(($"{name}.task", t.Task, max), ($"{name}.organisation", t.Organisation, max),
            ($"{name}.vocabulary", t.Vocabulary, max), ($"{name}.grammar", t.Grammar, max));
        Check("task1", ai.Task1, CefrScale.WritingTask1Max / 4);
        Check("task2", ai.Task2, CefrScale.WritingTask2Max / 4);
        var raw = ai.Task1.Total + ai.Task2.Total;
        var total = CefrScale.WritingScore(ai.Task1.Total, ai.Task2.Total);
        return new CefrWritingResult("cefr", "writing", set.Id, total, CefrScale.Max, CefrScale.Level(total), raw, CefrScale.RawMax, secondsUsed,
            [new("1", ai.Task1.Total, CefrScale.WritingTask1Max, ai.Task1), new("2", ai.Task2.Total, CefrScale.WritingTask2Max, ai.Task2)],
            [
                new("1.1", IeltsBand.CountWords(t11), CefrBank.Words11.Min, CefrBank.Words11.Max, t11),
                new("1.2", IeltsBand.CountWords(t12), CefrBank.Words12.Min, CefrBank.Words12.Max, t12),
                new("2", IeltsBand.CountWords(t2), CefrBank.Words2.Min, CefrBank.Words2.Max, t2),
            ],
            ai.TopCorrections.Take(5).ToList(), ai.Strengths, ai.NextSteps.Take(3).ToList());
    }
}
