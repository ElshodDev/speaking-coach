using System.Text.Json;
using System.Text.Json.Serialization;
namespace SpeakingCoach.Application;

/// <summary>IELTS Speaking Part 2 kartochkasi (trener uchun): mavzu, "You should say" bandlari, yakuniy "and explain…".</summary>
public record Part2Card(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("topic")] string Topic,
    [property: JsonPropertyName("points")] string[] Points,
    [property: JsonPropertyName("explain")] string Explain);

/// <summary>Kartochkaning bitta bandi javobda qamrab olinganmi.</summary>
public record CoverageItem(
    [property: JsonPropertyName("point")] string Point,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("evidence")] string Evidence);

/// <summary>
/// Part 2 natijasi. Speaking mashqi bilan bir xil asos (transkript, uch mezon, tuzatishlar) —
/// shuning uchun tarix, XP, takrorlash kartalari va ravonlik grafigi o'zgarishsiz ishlaydi;
/// qo'shimcha: bandlar qamrovi va taxminiy IELTS bali.
/// </summary>
public record Part2Evaluation(
    [property: JsonPropertyName("transcript")] string Transcript,
    [property: JsonPropertyName("fluency")] ScoreWithReasoning Fluency,
    [property: JsonPropertyName("grammar")] ScoreWithReasoning Grammar,
    [property: JsonPropertyName("vocabulary")] ScoreWithReasoning Vocabulary,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("encouragement")] string Encouragement,
    [property: JsonPropertyName("nextFocus")] string NextFocus,
    [property: JsonPropertyName("coverage")] List<CoverageItem> Coverage,
    [property: JsonPropertyName("estimatedBand")] decimal EstimatedBand);

/// <summary>Kartochkalar banki va natijani tekshirish qoidalari — toza funksiyalar (unit test qilinadi).</summary>
public static class Part2Rules
{
    public const int PrepSeconds = IeltsBank.Part2PrepSeconds;
    public const int SpeakSeconds = IeltsBank.Part2SpeakSeconds;

    public static readonly string[] Statuses = ["covered", "partly", "missed"];

    /// <summary>
    /// Trener uchun qo'shimcha original kartochkalar (mock to'plamlaridagi 2-qism kartochkalariga qo'shiladi).
    /// Mavzular IELTS'dagi odatiy turlar: odam, joy, narsa, voqea, faoliyat, tajriba.
    /// </summary>
    public static readonly Part2Card[] Extra =
    [
        new("c1", "Describe a person who taught you something important.", ["who this person is", "how you know them", "what they taught you"], "and explain why this lesson was important to you."),
        new("c2", "Describe a skill you learned as a child.", ["what the skill is", "when and how you learned it", "who helped you"], "and explain how this skill is useful to you now."),
        new("c3", "Describe a journey that you remember well.", ["where you went", "how you travelled", "who you were with"], "and explain why you remember this journey."),
        new("c4", "Describe a book or film that made you think.", ["what it was", "when you read or watched it", "what it was about"], "and explain why it made you think."),
        new("c5", "Describe a time when you helped someone.", ["who you helped", "what the situation was", "what you did"], "and explain how you felt afterwards."),
        new("c6", "Describe a piece of technology you use every day.", ["what it is", "when you started using it", "what you use it for"], "and explain how your life would be different without it."),
        new("c7", "Describe a celebration or festival in your country.", ["what it is called", "when it takes place", "what people do"], "and explain why it is important to people."),
        new("c8", "Describe a goal you would like to achieve in the future.", ["what the goal is", "why you chose it", "what you are doing to reach it"], "and explain how you will feel when you achieve it."),
        new("c9", "Describe a time you changed your mind about something.", ["what you first thought", "what made you change your mind", "what you think now"], "and explain whether changing your mind was a good decision."),
        new("c10", "Describe a meal you enjoyed with other people.", ["where you had it", "who you were with", "what you ate"], "and explain why you enjoyed this meal."),
        new("c11", "Describe a building that you find interesting.", ["where it is", "what it looks like", "what it is used for"], "and explain why you find it interesting."),
        new("c12", "Describe a time you were late for something important.", ["what the event was", "why you were late", "what happened as a result"], "and explain what you learned from this experience."),
        new("c13", "Describe a hobby you would like to try.", ["what the hobby is", "how you learned about it", "what you would need to start"], "and explain why you would like to try it."),
        new("c14", "Describe a piece of advice you received.", ["who gave you the advice", "what the advice was", "whether you followed it"], "and explain how it affected you."),
        new("c15", "Describe a place near water that you like.", ["where it is", "how often you go there", "what you do there"], "and explain why you like this place."),
        new("c16", "Describe a time you worked in a team.", ["what the task was", "who was in the team", "what your role was"], "and explain how well the team worked together."),
    ];

    /// <summary>Hamma kartochkalar: avval qo'shimchalar, keyin mock to'plamlaridagi 2-qism kartochkalari ("set:" + to'plam ID).</summary>
    public static List<Part2Card> All(IEnumerable<SpeakingSet> sets) =>
    [
        .. Extra,
        .. sets.Select(s => new Part2Card("set:" + s.Id, s.Part2.Topic, s.Part2.Points, s.Part2.Explain)),
    ];

    /// <summary>
    /// AI qamrovini kartochka bandlariga moslashtiradi: tartib — kartochkadagidek, holat — faqat
    /// covered/partly/missed, AI tushirib qoldirgan band — "missed". Oxirgi band — "and explain…".
    /// </summary>
    public static List<CoverageItem> NormalizeCoverage(Part2Card card, IEnumerable<CoverageItem>? ai)
    {
        var given = (ai ?? []).ToList();
        var points = card.Points.Append(card.Explain).ToList();
        return points.Select((p, i) =>
        {
            var match = given.FirstOrDefault(c => Same(c.Point, p)) ?? (i < given.Count ? given[i] : null);
            var status = match is not null && Statuses.Contains(match.Status?.Trim().ToLowerInvariant()) ? match.Status.Trim().ToLowerInvariant() : "missed";
            var evidence = status == "missed" ? "" : Cut(match?.Evidence ?? "", 200);
            return new CoverageItem(p, status, evidence);
        }).ToList();
    }

    /// <summary>IELTS bali: 0–9, yarim ballik qadam.</summary>
    public static decimal ClampBand(decimal band) => Math.Clamp(Math.Round(band * 2, MidpointRounding.AwayFromZero) / 2, 0, 9);

    /// <summary>Bandlar qamrovi foizda: covered — 1, partly — 0.5.</summary>
    public static int CoveragePercent(IReadOnlyCollection<CoverageItem> items) =>
        items.Count == 0 ? 0 : (int)Math.Round(100.0 * items.Sum(c => c.Status switch { "covered" => 1.0, "partly" => 0.5, _ => 0 }) / items.Count, MidpointRounding.AwayFromZero);

    private static bool Same(string? a, string b) =>
        string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);

    private static string Norm(string? s) => string.Join(' ', (s ?? "").Trim().TrimEnd('.').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Cut(string s, int max) => s.Length > max ? s[..max] + "…" : s;
}

public interface IPart2Coach
{
    Task<Part2Evaluation> EvaluateAsync(Part2Card card, byte[] audio, string mimeType, string level, string lang, CancellationToken ct = default);
}

/// <summary>Bitta Gemini so'rovi: transkript + uch mezon + bandlar qamrovi + taxminiy IELTS bali.</summary>
public class GeminiPart2Coach(IGeminiClient gemini) : IPart2Coach
{
    public static string BuildPrompt(Part2Card card, string level, string lang)
    {
        var bullets = string.Join("\n", card.Points.Select(p => "- " + p));
        return $$"""
            You are an IELTS Speaking examiner and coach for Uzbek- and Russian-speaking learners. The learner's level is {{LearnerLevel.Describe(level)}}.
            This is IELTS Speaking Part 2 (the long turn): the candidate had 1 minute to prepare and up to 2 minutes to speak.

            The cue card:
            {{card.Topic}}
            You should say:
            {{bullets}}
            {{card.Explain}}

            Listen to the attached audio. First transcribe it exactly as spoken, keeping hesitations ("uh", "um"),
            repetitions and self-corrections. Then evaluate ONLY what is in the audio — never invent content.

            1) Coverage: for EACH line of the cue card (each "You should say" point and the final "and explain…" line),
               decide "covered" (clearly answered), "partly" (mentioned briefly or vaguely) or "missed",
               and quote the candidate's exact words as evidence (empty string if missed).
            2) Scores 0-100 (same scale as everyday practice): fluency (pace, hesitation, how long they kept going,
               coherence and linking), grammar (range and accuracy), vocabulary (range, topic words, collocations).
               Speaking for clearly less than a minute must lower fluency.
            3) estimatedBand: one overall IELTS Speaking band for this long turn (0-9, may end in .5). It is an estimate.
            Write every "reasoning", "explanation", "encouragement" and "nextFocus" in {{GeminiIeltsEvaluator.FeedbackLanguage(lang)}}.
            "nextFocus" must be specific to Part 2 (for example structure, covering every point, extending ideas, using the full 2 minutes).

            Return ONLY valid JSON, no markdown fences, in exactly this shape:
            {
              "transcript": "<exact transcription>",
              "fluency": {"score": <0-100>, "reasoning": "<cite specific words or pauses>"},
              "grammar": {"score": <0-100>, "reasoning": "<cite exact errors>"},
              "vocabulary": {"score": <0-100>, "reasoning": "<cite specific words>"},
              "topCorrections": [{"original": "<exact phrase said>", "corrected": "<better version>", "explanation": "<one sentence>"}],
              "encouragement": "<one genuine, specific strength>",
              "nextFocus": "<the single most useful thing to practise next for Part 2>",
              "coverage": [{"point": "<cue card line>", "status": "covered|partly|missed", "evidence": "<exact quote or empty>"}],
              "estimatedBand": <0-9>
            }
            """;
    }

    public async Task<Part2Evaluation> EvaluateAsync(Part2Card card, byte[] audio, string mimeType, string level, string lang, CancellationToken ct = default)
    {
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = BuildPrompt(card, level, lang) },
                        new { inline_data = new { mime_type = mimeType.Split(';')[0], data = Convert.ToBase64String(audio) } },
                    },
                },
            },
            generationConfig = new { temperature = 0.2, responseMimeType = "application/json" },
        };
        var response = await gemini.SendWithFallbackAsync(body, ct);
        var text = await GeminiJson.ExtractTextAsync(response, ct);
        var r = GeminiJson.DeserializeStrict<Part2Evaluation>(text);
        ScoreGuard.EnsureValid(("fluency", r.Fluency), ("grammar", r.Grammar), ("vocabulary", r.Vocabulary));
        return r with
        {
            TopCorrections = r.TopCorrections ?? [],
            Coverage = Part2Rules.NormalizeCoverage(card, r.Coverage),
            EstimatedBand = Part2Rules.ClampBand(r.EstimatedBand),
        };
    }
}
