using System.Text;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services.Mock;

public record PartScore(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("reasoning")] string Reasoning);

/// <summary>Speaking: rasmiy baholash shkalasi guruhlari (1–3, 4–6, 7, 8).</summary>
public record CefrSpeakingAi(
    [property: JsonPropertyName("answers")] List<AnswerTranscript> Answers,
    [property: JsonPropertyName("q1_3")] PartScore Q1To3,
    [property: JsonPropertyName("q4_6")] PartScore Q4To6,
    [property: JsonPropertyName("q7")] PartScore Q7,
    [property: JsonPropertyName("q8")] PartScore Q8,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

/// <summary>Writing: 5 ta rasmiy mezon bo'yicha izohlar.</summary>
public record CefrCriteria(
    [property: JsonPropertyName("taskCompletion")] string TaskCompletion,
    [property: JsonPropertyName("grammar")] string Grammar,
    [property: JsonPropertyName("vocabulary")] string Vocabulary,
    [property: JsonPropertyName("coherence")] string Coherence,
    [property: JsonPropertyName("punctuation")] string Punctuation);

public record CefrWritingPartAi(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("reasoning")] string Reasoning,
    [property: JsonPropertyName("criteria")] CefrCriteria Criteria);

public record CefrWritingAi(
    [property: JsonPropertyName("part1")] CefrWritingPartAi Part1,
    [property: JsonPropertyName("part2")] CefrWritingPartAi Part2,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

public record CefrGroupResult(string Questions, string Part, int Score, int Max, string Reasoning);
public record CefrAnswer(int Index, string Part, string Question, string Transcript, int Seconds);

public record CefrSpeakingResult(
    string Exam, string Module, string SetId, int Overall, int Max, string Level, int Raw, int RawMax,
    List<CefrGroupResult> Groups, List<CefrAnswer> Answers, List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

public record CefrText(string Task, int Words, int MinWords, int MaxWords, string Text);
public record CefrWritingPart(string Part, int Score, int Max, string Reasoning, CefrCriteria Criteria);

public record CefrWritingResult(
    string Exam, string Module, string SetId, int Overall, int Max, string Level, int Raw, int RawMax, int SecondsUsed,
    List<CefrWritingPart> Parts, List<CefrText> Texts, List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

public interface ICefrEvaluator
{
    Task<CefrSpeakingResult> EvaluateSpeakingAsync(CefrSpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang, CancellationToken ct = default);
    Task<CefrWritingResult> EvaluateWritingAsync(CefrWritingSet set, string t11, string t12, string t2, int secondsUsed, string lang, CancellationToken ct = default);
}

/// <summary>
/// CEFR (Multilevel) mock'ni Gemini bilan baholash — Bilim va malakalarni
/// baholash agentligining rasmiy hujjatlari bo'yicha (manbalar — CefrBank.cs):
/// Speaking — rasmiy "Rating scale" (1–3: 0–5, 4–6: 0–5, 7: 0–5, 8: 0–6);
/// Writing — 1-qism 12 ball, 2-qism 24 ball, 5 ta rasmiy mezon, 0–36 → 75
/// rasmiy jadvali. Yakuniy ball va darajani server hisoblaydi.
/// </summary>
public class GeminiCefrEvaluator(GeminiClient gemini) : ICefrEvaluator
{
    private const string Examiner = """
        You are a strict but fair examiner for Uzbekistan's national multilevel (CEFR-based) English test,
        run by the Agency for Assessment of Knowledge and Skills. Judge ONLY what is in the response,
        cite the candidate's exact words, and do not inflate scores.

        """;

    /// <summary>Rasmiy "Rating scale for multilevel speaking exams" (yangi format) — qisqartirilmagan mazmuni.</summary>
    public const string OfficialSpeakingScale = """
        OFFICIAL RATING SCALE (use it exactly):
        Questions 1-3 (Part 1.1), 0-5:
          5 = performance likely above A2. 4 = Higher A2: all three responses on topic; correct simple structures, adequate vocabulary, noticeable mispronunciations and frequent pausing but meaning clear.
          3 = Lower A2: two responses on topic, otherwise as 4. 2 = Higher A1: at least two on topic; limited grammar, basic vocabulary, mostly unintelligible pronunciation, pausing impedes understanding.
          1 = Lower A1: one response on topic, otherwise as 2. 0 = no meaningful language or all completely off topic.
        Questions 4-6 (Part 1.2), 0-5:
          5 = likely above B1. 4 = Higher B1: all three on topic; correct simple structures, sufficient vocabulary control, generally intelligible pronunciation, some pausing, simple cohesive devices.
          3 = Lower B1: two on topic, otherwise as 4. 2 = Higher A2: at least two on topic; systematic basic mistakes, noticeable inappropriate word choice, frequent mispronunciation, limited cohesion.
          1 = Lower A2: one on topic, otherwise as 2. 0 = below A2, no meaningful language, or all off topic.
        Question 7 (Part 2), 0-5:
          5 = likely above B2. 4 = Higher B2: all three questions answered on topic; accurate complex grammar, sufficient vocabulary range, intelligible pronunciation, minimal pausing, limited cohesive devices.
          3 = Lower B2: two questions answered, otherwise as 4. 2 = Higher B1: at least two answered; correct simple structures, vocabulary limitations, generally intelligible, simple cohesive devices.
          1 = Lower B1: one answered, otherwise as 2. 0 = below B1, no meaningful language, or off topic.
        Question 8 (Part 3), 0-6:
          6 = likely above C1. 5 = C1: clear presentation highlighting points from each section, reasons for and against; accurate complex grammar, range of vocabulary, intelligible pronunciation, range of cohesive devices.
          4 = Higher B2: addresses points from each section; accurate complex grammar, sufficient vocabulary, limited cohesive devices.
          3 = Lower B2: addresses points from only one section, otherwise as 4.
          2 = Higher B1: unable to construct a coherent, sustained response, heavily dependent on the prompts.
          1 = Lower B1: unable to construct a coherent response and reads directly from the prompts. 0 = below B1, no meaningful language, or off topic.
        """;

    public static string BuildSpeakingPrompt(CefrSpeakingSet set, string lang)
    {
        var sb = new StringBuilder(Examiner);
        sb.AppendLine("This is a full multilevel Speaking mock in the official new format. Each question is followed by the candidate's recorded answer (audio); some may be missing.");
        sb.AppendLine($"Part 1.2 pictures: {string.Join("; ", set.Pictures.Select(p => p.Caption))}.");
        sb.AppendLine($"Part 2 (question 7): the candidate saw one picture ({set.Part2Picture.Caption}; describing it is NOT required) and answered in one turn: {string.Join(" ", set.Part2Questions)}");
        sb.AppendLine($"Part 3 (question 8): statement \"{set.Part3Statement}\"; FOR section: {string.Join("; ", set.For)}; AGAINST section: {string.Join("; ", set.Against)}.");
        sb.AppendLine("First transcribe every answer exactly (keep hesitations). Then score with the official scale:");
        sb.AppendLine(OfficialSpeakingScale);
        sb.AppendLine($"Write every \"reasoning\", \"strengths\", \"nextSteps\" item and correction \"explanation\" in {GeminiIeltsEvaluator.FeedbackLanguage(lang)}; name the level descriptor you applied.");
        sb.AppendLine("""
            Return ONLY valid JSON, no markdown fences:
            {"answers": [{"index": <int>, "transcript": "<exact transcription or empty string>"}],
             "q1_3": {"score": <0-5>, "reasoning": "..."}, "q4_6": {"score": <0-5>, "reasoning": "..."},
             "q7": {"score": <0-5>, "reasoning": "..."}, "q8": {"score": <0-6>, "reasoning": "..."},
             "topCorrections": [{"original": "...", "corrected": "...", "explanation": "..."}],
             "strengths": "...", "nextSteps": ["...", "...", "..."]}
            Give at most 5 topCorrections.
            """);
        return sb.ToString();
    }

    public static string BuildWritingPrompt(CefrWritingSet set, string t11, string t12, string t2, string lang)
    {
        var sb = new StringBuilder(Examiner);
        sb.AppendLine("This is a full multilevel Writing mock in the official new format:");
        sb.AppendLine($"PART 1 (worth {CefrScale.WritingTask1Max} points) = task 1.1, an INFORMAL letter/email to a friend of about 50 words (B1), plus task 1.2, a FORMAL letter of 120-150 words (B2), both about the same situation. Score Part 1 as a whole.");
        sb.AppendLine($"PART 2 (worth {CefrScale.WritingTask2Max} points) = an online discussion post of 180-200 words giving an opinion with reasons and examples (C1).");
        sb.AppendLine("Use the five official criteria: task completion, grammatical structures, vocabulary range, coherence and cohesion, punctuation and spelling.");
        sb.AppendLine("Give each part one holistic score and a short comment for each criterion. Wrong register, far-off length or off-topic answers lose marks for task completion; an empty part gets 0.");
        sb.AppendLine($"Write every \"reasoning\", criterion comment, \"strengths\", \"nextSteps\" item and correction \"explanation\" in {GeminiIeltsEvaluator.FeedbackLanguage(lang)}.");
        sb.AppendLine();
        sb.AppendLine("=== SITUATION ===");
        sb.AppendLine(set.Role);
        sb.AppendLine(set.Email);
        void Task(string name, string prompt, string text)
        {
            sb.AppendLine($"=== TASK {name} ===");
            sb.AppendLine(prompt);
            sb.AppendLine($"--- ANSWER ({IeltsBand.CountWords(text)} words) ---");
            sb.AppendLine(string.IsNullOrWhiteSpace(text) ? "(no answer)" : text);
            sb.AppendLine();
        }
        Task("1.1", set.Task11, t11);
        Task("1.2", set.Task12, t12);
        Task("2", set.Task2, t2);
        sb.AppendLine("""
            Return ONLY valid JSON, no markdown fences:
            {"part1": {"score": <0-12>, "reasoning": "...", "criteria": {"taskCompletion": "...", "grammar": "...", "vocabulary": "...", "coherence": "...", "punctuation": "..."}},
             "part2": {"score": <0-24>, "reasoning": "...", "criteria": {same five keys}},
             "topCorrections": [{"original": "...", "corrected": "...", "explanation": "..."}],
             "strengths": "...", "nextSteps": ["...", "...", "..."]}
            Give at most 5 topCorrections.
            """);
        return sb.ToString();
    }

    public static void EnsureScores(params (string Name, int Score, string Reasoning, int Max)[] scores)
    {
        foreach (var (name, score, reasoning, max) in scores)
        {
            if (score < 0 || score > max) throw new InvalidOperationException($"Gemini '{name}' uchun 0-{max} dan tashqari ball: {score}");
            if (string.IsNullOrWhiteSpace(reasoning)) throw new InvalidOperationException($"Gemini '{name}' uchun izoh bermadi");
        }
    }

    public async Task<CefrSpeakingResult> EvaluateSpeakingAsync(CefrSpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang, CancellationToken ct = default)
    {
        var questions = set.Questions();
        var parts = new List<object> { new { text = BuildSpeakingPrompt(set, lang) } };
        for (var i = 0; i < questions.Count; i++)
        {
            parts.Add(new { text = $"Question {i + 1} (Part {questions[i].Part}; answer index {i}): {questions[i].Text}" });
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
        var groups = new List<CefrGroupResult>
        {
            new("1–3", "1.1", ai.Q1To3.Score, CefrScale.SpeakingMax[0], ai.Q1To3.Reasoning),
            new("4–6", "1.2", ai.Q4To6.Score, CefrScale.SpeakingMax[1], ai.Q4To6.Reasoning),
            new("7", "2", ai.Q7.Score, CefrScale.SpeakingMax[2], ai.Q7.Reasoning),
            new("8", "3", ai.Q8.Score, CefrScale.SpeakingMax[3], ai.Q8.Reasoning),
        };
        EnsureScores(groups.Select(g => (g.Questions, g.Score, g.Reasoning, g.Max)).ToArray());
        var raw = groups.Sum(g => g.Score);
        var total = CefrScale.SpeakingScore(groups.Select(g => g.Score).ToList());
        var list = set.Questions().Select((q, i) =>
        {
            var spoken = answers.FirstOrDefault(x => x.Index == i);
            var transcript = spoken?.Audio is { Length: > 0 } ? ai.Answers.FirstOrDefault(t => t.Index == i)?.Transcript ?? "" : "";
            return new CefrAnswer(i, q.Part, q.Text, transcript, spoken?.Seconds ?? 0);
        }).ToList();
        return new CefrSpeakingResult("cefr", "speaking", set.Id, total, CefrScale.Max, CefrScale.Level(total), raw, CefrScale.SpeakingRawMax,
            groups, list, ai.TopCorrections.Take(5).ToList(), ai.Strengths, ai.NextSteps.Take(3).ToList());
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
        EnsureScores(("part1", ai.Part1.Score, ai.Part1.Reasoning, CefrScale.WritingTask1Max),
            ("part2", ai.Part2.Score, ai.Part2.Reasoning, CefrScale.WritingTask2Max));
        var raw = ai.Part1.Score + ai.Part2.Score;
        var total = CefrScale.WritingScore(ai.Part1.Score, ai.Part2.Score);
        return new CefrWritingResult("cefr", "writing", set.Id, total, CefrScale.Max, CefrScale.Level(total), raw, CefrScale.RawMax, secondsUsed,
            [
                new("1", ai.Part1.Score, CefrScale.WritingTask1Max, ai.Part1.Reasoning, ai.Part1.Criteria),
                new("2", ai.Part2.Score, CefrScale.WritingTask2Max, ai.Part2.Reasoning, ai.Part2.Criteria),
            ],
            [
                new("1.1", IeltsBand.CountWords(t11), CefrBank.Words11.Min, CefrBank.Words11.Max, t11),
                new("1.2", IeltsBand.CountWords(t12), CefrBank.Words12.Min, CefrBank.Words12.Max, t12),
                new("2", IeltsBand.CountWords(t2), CefrBank.Words2.Min, CefrBank.Words2.Max, t2),
            ],
            ai.TopCorrections.Take(5).ToList(), ai.Strengths, ai.NextSteps.Take(3).ToList());
    }
}
