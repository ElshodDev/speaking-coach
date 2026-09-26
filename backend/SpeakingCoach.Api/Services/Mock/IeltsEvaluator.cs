using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services.Mock;

// ---- Gemini javobining shakli (qat'iy o'qiladi) ----

public record BandWithReasoning(
    [property: JsonPropertyName("band")] int Band,
    [property: JsonPropertyName("reasoning")] string Reasoning);

public record AnswerTranscript(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("transcript")] string Transcript);

public record SpeakingMockAi(
    [property: JsonPropertyName("answers")] List<AnswerTranscript> Answers,
    [property: JsonPropertyName("fluencyCoherence")] BandWithReasoning FluencyCoherence,
    [property: JsonPropertyName("lexicalResource")] BandWithReasoning LexicalResource,
    [property: JsonPropertyName("grammaticalRange")] BandWithReasoning GrammaticalRange,
    [property: JsonPropertyName("pronunciation")] BandWithReasoning Pronunciation,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

public record WritingTaskAi(
    [property: JsonPropertyName("task")] BandWithReasoning Task,
    [property: JsonPropertyName("coherenceCohesion")] BandWithReasoning CoherenceCohesion,
    [property: JsonPropertyName("lexicalResource")] BandWithReasoning LexicalResource,
    [property: JsonPropertyName("grammaticalRange")] BandWithReasoning GrammaticalRange);

public record WritingMockAi(
    [property: JsonPropertyName("task1")] WritingTaskAi Task1,
    [property: JsonPropertyName("task2")] WritingTaskAi Task2,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("strengths")] string Strengths,
    [property: JsonPropertyName("nextSteps")] List<string> NextSteps);

// ---- Saqlanadigan va foydalanuvchiga qaytadigan natija ----

public record SpeakingAnswer(int Index, int Part, string Question, string Transcript, int Seconds);

public record SpeakingMockResult(
    string Exam, string Module, string SetId, decimal Overall,
    BandWithReasoning FluencyCoherence, BandWithReasoning LexicalResource,
    BandWithReasoning GrammaticalRange, BandWithReasoning Pronunciation,
    List<SpeakingAnswer> Answers, List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

public record WritingTaskResult(decimal Band, int Words, int MinWords, string Text, WritingTaskAi Criteria);

public record WritingMockResult(
    string Exam, string Module, string SetId, string Variant, decimal Overall, int SecondsUsed,
    WritingTaskResult Task1, WritingTaskResult Task2,
    List<CorrectionItem> TopCorrections, string Strengths, List<string> NextSteps);

/// <summary>Bitta javob: qaysi savolga, audio (bo'lmasa — javob berilmagan), davomiyligi.</summary>
public record SpokenAnswer(int Index, byte[]? Audio, string? MimeType, int Seconds);

public interface IIeltsEvaluator
{
    Task<SpeakingMockResult> EvaluateSpeakingAsync(SpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang, CancellationToken ct = default);
    Task<WritingMockResult> EvaluateWritingAsync(WritingSet set, string task1, string task2, int secondsUsed, string lang, CancellationToken ct = default);
}

/// <summary>
/// IELTS mock'ni Gemini bilan baholash. Butun imtihon BITTA so'rovda —
/// imtihonchi ham nutqni bir butun sifatida baholaydi, va kvota tejaladi.
/// Band'larni AI beradi (mezon bo'yicha, butun son), umumiy natijani esa
/// server o'zi hisoblaydi (IeltsBand) — arifmetikani AI'ga ishonmaymiz.
/// </summary>
public class GeminiIeltsEvaluator(GeminiClient gemini) : IIeltsEvaluator
{
    public static string FeedbackLanguage(string lang) => lang switch
    {
        "ru" => "Russian",
        "en" => "simple English",
        _ => "Uzbek (Latin script; use the letters oʻ and gʻ)",
    };

    private const string Honesty = """
        Be a strict but fair certified IELTS examiner. Use the official public IELTS band descriptors.
        Judge ONLY what is actually in the response. Never invent content. Do not inflate bands to be kind:
        an accurate estimate is what helps the learner.
        """;

    public static string BuildSpeakingPrompt(SpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Honesty);
        sb.AppendLine("This is a full IELTS Speaking mock test (Part 1 interview, Part 2 long turn, Part 3 discussion).");
        sb.AppendLine("Below, each question is followed by the candidate's recorded answer (audio). Some answers may be missing.");
        sb.AppendLine("First transcribe every answer exactly as spoken (keep hesitations such as \"uh\", repetitions and self-corrections).");
        sb.AppendLine("Then give ONE band (a whole number 0-9) per criterion for the whole test:");
        sb.AppendLine("fluency and coherence, lexical resource, grammatical range and accuracy, pronunciation.");
        sb.AppendLine("A missing or very short answer must lower fluency and coherence. Part 2 should last close to 2 minutes.");
        sb.AppendLine($"Write every \"reasoning\", \"strengths\", \"nextSteps\" item and correction \"explanation\" in {FeedbackLanguage(lang)}.");
        sb.AppendLine("In reasoning, quote the candidate's exact words as evidence.");
        sb.AppendLine("""
            Return ONLY valid JSON, no markdown fences, in exactly this shape:
            {
              "answers": [{"index": <question index>, "transcript": "<exact transcription, or empty string if no audio>"}],
              "fluencyCoherence": {"band": <0-9>, "reasoning": "<evidence>"},
              "lexicalResource": {"band": <0-9>, "reasoning": "<evidence>"},
              "grammaticalRange": {"band": <0-9>, "reasoning": "<evidence>"},
              "pronunciation": {"band": <0-9>, "reasoning": "<evidence>"},
              "topCorrections": [{"original": "<exact phrase said>", "corrected": "<better version>", "explanation": "<one sentence>"}],
              "strengths": "<one or two specific strengths>",
              "nextSteps": ["<the most useful thing to practise to reach the next band>", "<second>", "<third>"]
            }
            Give at most 5 topCorrections, choosing the most important errors.
            """);
        return sb.ToString();
    }

    public static string BuildWritingPrompt(WritingSet set, string task1, string task2, string lang)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Honesty);
        var variant = set.Variant == IeltsBank.Academic ? "Academic" : "General Training";
        sb.AppendLine($"This is a full IELTS {variant} Writing mock test. Assess both tasks with the official band descriptors.");
        sb.AppendLine($"Task 1 needs at least {IeltsBank.Task1MinWords} words, Task 2 at least {IeltsBank.Task2MinWords} words; an answer under the minimum must be penalised in task achievement / task response.");
        sb.AppendLine("Give a whole band 0-9 per criterion. For Task 1 \"task\" means Task Achievement; for Task 2 it means Task Response.");
        sb.AppendLine("An empty or off-topic answer gets band 0 or 1 for that task.");
        sb.AppendLine($"Write every \"reasoning\", \"strengths\", \"nextSteps\" item and correction \"explanation\" in {FeedbackLanguage(lang)}. Quote the candidate's exact words as evidence.");
        sb.AppendLine();
        sb.AppendLine("=== TASK 1 PROMPT ===");
        sb.AppendLine(set.Task1.Prompt);
        if (set.Task1.Chart is { } c)
        {
            sb.AppendLine($"Chart data ({c.Title}, unit: {c.Unit}):");
            sb.AppendLine("Category | " + string.Join(" | ", c.Series.Select(s => s.Name)));
            for (var i = 0; i < c.Categories.Length; i++)
            {
                sb.AppendLine(c.Categories[i] + " | " + string.Join(" | ", c.Series.Select(s => s.Values[i].ToString(CultureInfo.InvariantCulture))));
            }
            sb.AppendLine("Check that the numbers the candidate reports match this data.");
        }
        sb.AppendLine($"=== TASK 1 ANSWER ({IeltsBand.CountWords(task1)} words) ===");
        sb.AppendLine(string.IsNullOrWhiteSpace(task1) ? "(no answer)" : task1);
        sb.AppendLine();
        sb.AppendLine("=== TASK 2 PROMPT ===");
        sb.AppendLine(set.Task2);
        sb.AppendLine($"=== TASK 2 ANSWER ({IeltsBand.CountWords(task2)} words) ===");
        sb.AppendLine(string.IsNullOrWhiteSpace(task2) ? "(no answer)" : task2);
        sb.AppendLine("""

            Return ONLY valid JSON, no markdown fences, in exactly this shape:
            {
              "task1": {"task": {"band": <0-9>, "reasoning": "..."}, "coherenceCohesion": {"band": <0-9>, "reasoning": "..."},
                        "lexicalResource": {"band": <0-9>, "reasoning": "..."}, "grammaticalRange": {"band": <0-9>, "reasoning": "..."}},
              "task2": {"task": {"band": <0-9>, "reasoning": "..."}, "coherenceCohesion": {"band": <0-9>, "reasoning": "..."},
                        "lexicalResource": {"band": <0-9>, "reasoning": "..."}, "grammaticalRange": {"band": <0-9>, "reasoning": "..."}},
              "topCorrections": [{"original": "<exact phrase written>", "corrected": "<better version>", "explanation": "<one sentence>"}],
              "strengths": "<one or two specific strengths>",
              "nextSteps": ["<most useful thing to practise to reach the next band>", "<second>", "<third>"]
            }
            Give at most 5 topCorrections.
            """);
        return sb.ToString();
    }

    public static void EnsureBands(params (string Name, BandWithReasoning B)[] bands)
    {
        foreach (var (name, b) in bands)
        {
            if (b.Band is < 0 or > 9) throw new InvalidOperationException($"Gemini '{name}' uchun 0-9 dan tashqari band qaytardi: {b.Band}");
            if (string.IsNullOrWhiteSpace(b.Reasoning)) throw new InvalidOperationException($"Gemini '{name}' uchun izoh bermadi");
        }
    }

    public async Task<SpeakingMockResult> EvaluateSpeakingAsync(SpeakingSet set, IReadOnlyList<SpokenAnswer> answers, string lang, CancellationToken ct = default)
    {
        var questions = set.Questions();
        var parts = new List<object> { new { text = BuildSpeakingPrompt(set, answers, lang) } };
        for (var i = 0; i < questions.Count; i++)
        {
            var a = answers.FirstOrDefault(x => x.Index == i);
            var label = questions[i].Part == 2
                ? $"Question {i} (Part 2 cue card): {set.Part2.Topic} You should say: {string.Join("; ", set.Part2.Points)}; {set.Part2.Explain}"
                : $"Question {i} (Part {questions[i].Part}): {questions[i].Text}";
            parts.Add(new { text = label });
            if (a?.Audio is { Length: > 0 } audio)
            {
                parts.Add(new { inline_data = new { mime_type = (a.MimeType ?? "audio/webm").Split(';')[0], data = Convert.ToBase64String(audio) } });
            }
            else
            {
                parts.Add(new { text = "(no answer recorded)" });
            }
        }

        var body = new
        {
            contents = new[] { new { parts = parts.ToArray() } },
            generationConfig = new { temperature = 0.2, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var ai = GeminiClient.DeserializeStrict<SpeakingMockAi>(await GeminiClient.ExtractTextAsync(response, ct));
        EnsureBands(("fluencyCoherence", ai.FluencyCoherence), ("lexicalResource", ai.LexicalResource),
            ("grammaticalRange", ai.GrammaticalRange), ("pronunciation", ai.Pronunciation));

        var answered = questions.Select((q, i) =>
        {
            var spoken = answers.FirstOrDefault(x => x.Index == i);
            var transcript = spoken?.Audio is { Length: > 0 } ? ai.Answers.FirstOrDefault(t => t.Index == i)?.Transcript ?? "" : "";
            return new SpeakingAnswer(i, q.Part, q.Text, transcript, spoken?.Seconds ?? 0);
        }).ToList();

        return new SpeakingMockResult("ielts", "speaking", set.Id,
            IeltsBand.Speaking(ai.FluencyCoherence.Band, ai.LexicalResource.Band, ai.GrammaticalRange.Band, ai.Pronunciation.Band),
            ai.FluencyCoherence, ai.LexicalResource, ai.GrammaticalRange, ai.Pronunciation,
            answered, ai.TopCorrections.Take(5).ToList(), ai.Strengths, ai.NextSteps.Take(3).ToList());
    }

    public async Task<WritingMockResult> EvaluateWritingAsync(WritingSet set, string task1, string task2, int secondsUsed, string lang, CancellationToken ct = default)
    {
        var body = new
        {
            contents = new[] { new { parts = new object[] { new { text = BuildWritingPrompt(set, task1, task2, lang) } } } },
            generationConfig = new { temperature = 0.2, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var ai = GeminiClient.DeserializeStrict<WritingMockAi>(await GeminiClient.ExtractTextAsync(response, ct));
        foreach (var (name, t) in new[] { ("task1", ai.Task1), ("task2", ai.Task2) })
        {
            EnsureBands(($"{name}.task", t.Task), ($"{name}.coherence", t.CoherenceCohesion),
                ($"{name}.lexical", t.LexicalResource), ($"{name}.grammar", t.GrammaticalRange));
        }

        return BuildWritingResult(set, task1, task2, secondsUsed, ai);
    }

    /// <summary>AI band'laridan yakuniy natija — toza funksiya (unit test qilinadi).</summary>
    public static WritingMockResult BuildWritingResult(WritingSet set, string task1, string task2, int secondsUsed, WritingMockAi ai)
    {
        static decimal Raw(WritingTaskAi t) => IeltsBand.TaskRaw(t.Task.Band, t.CoherenceCohesion.Band, t.LexicalResource.Band, t.GrammaticalRange.Band);
        var r1 = Raw(ai.Task1);
        var r2 = Raw(ai.Task2);
        return new WritingMockResult("ielts", "writing", set.Id, set.Variant, IeltsBand.Writing(r1, r2), secondsUsed,
            new WritingTaskResult(IeltsBand.RoundHalfBand(r1), IeltsBand.CountWords(task1), IeltsBank.Task1MinWords, task1, ai.Task1),
            new WritingTaskResult(IeltsBand.RoundHalfBand(r2), IeltsBand.CountWords(task2), IeltsBank.Task2MinWords, task2, ai.Task2),
            ai.TopCorrections.Take(5).ToList(), ai.Strengths, ai.NextSteps.Take(3).ToList());
    }
}
