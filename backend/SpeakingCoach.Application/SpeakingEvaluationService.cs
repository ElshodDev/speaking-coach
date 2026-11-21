using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Application;

// Bu record Gemini'dan qaytadigan JSON'ga aynan mos keladi.
// Property nomini o'zgartirsangiz, PromptTemplate'dagi OUTPUT FORMAT
// qismini ham yangilang — ikkalasi sinxron turishi kerak.
public record SpeakingEvaluationResult(
    [property: JsonPropertyName("transcript")] string Transcript,
    [property: JsonPropertyName("fluency")] ScoreWithReasoning Fluency,
    [property: JsonPropertyName("grammar")] ScoreWithReasoning Grammar,
    [property: JsonPropertyName("vocabulary")] ScoreWithReasoning Vocabulary,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("encouragement")] string Encouragement,
    [property: JsonPropertyName("nextFocus")] string NextFocus,
    // Quyidagilar IXTIYORIY (Features:SpeakingFourCriteria yoqiq bo'lsa to'ldiriladi; eski natijalarda yo'q).
    [property: JsonPropertyName("pronunciation")] ScoreWithReasoning? Pronunciation = null,
    [property: JsonPropertyName("bands")] SpeakingBands? Bands = null,
    [property: JsonPropertyName("priorityFix")] PriorityFix? PriorityFix = null,
    [property: JsonPropertyName("overallBand")] decimal? OverallBand = null);

public interface ISpeakingEvaluationService
{
    Task<SpeakingEvaluationResult> EvaluateAsync(
        string topic, byte[] audioBytes, string audioMimeType, string level, CancellationToken ct = default);
}

/// <summary>
/// Gemini multimodal model — audio faylni to'g'ridan-to'g'ri qabul qiladi,
/// shuning uchun alohida Speech-to-Text (Whisper) qatlami kerak emas:
/// transkripsiya va baholash bitta so'rovda amalga oshadi.
/// Google AI Studio (aistudio.google.com/apikey) bepul API kalit beradi,
/// kredit karta talab qilmaydi — shuning uchun bu yerda tanlangan.
///
/// So'rovni Gemini'ga yuborish (model fallback + retry) endi IGeminiClient
/// ichida — bu klass faqat PROMPT MATNI va NATIJANI O'QISH bilan shug'ullanadi
/// (Writing bilan solishtiring: WritingEvaluationService.cs xuddi shu
/// IGeminiClient'ni ishlatadi, faqat boshqa prompt va boshqa natija shakli
/// bilan).
/// </summary>
public class GeminiSpeakingService : ISpeakingEvaluationService
{
    private readonly IGeminiClient _geminiClient;
    private readonly FeatureFlags? _flags;

    private const string PromptTemplate = """
        You are an English speaking coach for Uzbek-speaking learners. This learner's level is {1}.
        Keep the scores on the absolute rubric below (so progress stays comparable across levels),
        but write explanations, corrections and the next focus in language a {1} learner understands.
        Listen to the attached audio recording. First transcribe it exactly as spoken
        (including hesitations and repeated words), then evaluate it using ONLY what
        is in the audio. Never invent information.

        Topic the learner was asked to speak about: {0}

        SCORING RUBRIC (0-100 for each):
        - Fluency: hesitation markers, filler words, pace, self-corrections.
          90-100=near-native flow, 70-89=minor pauses, 50-69=frequent restarts, <50=fragmented.
        - Grammar: tense consistency, subject-verb agreement, article usage
          (Uzbek has no articles, so a/an/the errors are expected and NOT
          penalized as heavily as verb tense or word order errors).
        - Vocabulary: range and appropriateness for the topic; penalize repetition
          of the same 5-6 words, reward topic-specific vocabulary.

        Return ONLY valid JSON matching this exact shape, no other text, no markdown fences:
        {{
          "transcript": "<exact transcription of what was said>",
          "fluency": {{"score": <0-100>, "reasoning": "<cite specific words/pauses>"}},
          "grammar": {{"score": <0-100>, "reasoning": "<cite specific errors, exact quote>"}},
          "vocabulary": {{"score": <0-100>, "reasoning": "<specific words used>"}},
          "topCorrections": [
            {{"original": "<exact phrase>", "corrected": "<fixed version>", "explanation": "<simple, one sentence>"}}
          ],
          "encouragement": "<one genuine, specific positive observation>",
          "nextFocus": "<single most impactful thing to practice next>"
        }}
        """;

    /// <summary>
    /// Yangi qism (Features:SpeakingFourCriteria): 4 rasmiy mezon bo'yicha taxminiy band, talaffuz va bitta asosiy
    /// tuzatish. Eski 0–100 ballar va ularning mezonlari o'zgarmaydi (progress grafigi uzluksiz qoladi).
    /// </summary>
    private const string FourCriteriaRubric = """

        IELTS-STYLE BANDS (in addition to the 0-100 scores above):
        Also give ONE band per official IELTS Speaking criterion, a number from 0 to 9 in steps of 0.5:
        fluency (Fluency and Coherence), lexical (Lexical Resource), grammar (Grammatical Range and Accuracy), pronunciation.
        These are rough estimates from a short recording, NOT an official result. Do not inflate them to be kind.
        Pronunciation is judged only from what you can hear: clarity of individual sounds, word stress and intonation.
        If the recording is short or noisy, stay conservative and say so in the pronunciation reasoning.

        PRIORITY FIX: choose exactly ONE thing that, if fixed, would help this learner the most.
        Quote the exact phrase they said, give the corrected version, and one short tip (a rule or a mini-drill)
        in language a {1} learner understands.
        """;

    private const string FourCriteriaShape = """
          "pronunciation": {{"score": <0-100>, "reasoning": "<specific words that were unclear or well said>"}},
          "bands": {{"fluency": <0-9>, "lexical": <0-9>, "grammar": <0-9>, "pronunciation": <0-9>}},
          "priorityFix": {{"issue": "<what to fix, one short sentence>", "original": "<exact phrase said>", "corrected": "<fixed version>", "tip": "<one short rule or drill>"}},
        """;

    /// <summary>Prompt'ni yig'adi — alohida metod, chunki uni unit test bilan tekshirish mumkin.</summary>
    public static string BuildPrompt(string topic, string level, bool fourCriteria = false)
    {
        var template = PromptTemplate;
        if (fourCriteria)
        {
            // Rubrikani JSON shaklidan oldin, yangi maydonlarni "vocabulary" qatoridan keyin qo'shamiz.
            template = template.Replace("Return ONLY valid JSON", FourCriteriaRubric.TrimStart('\n') + "\n\nReturn ONLY valid JSON");
            template = template.Replace(
                "\"vocabulary\": {{\"score\": <0-100>, \"reasoning\": \"<specific words used>\"}},\n",
                "\"vocabulary\": {{\"score\": <0-100>, \"reasoning\": \"<specific words used>\"}},\n" + FourCriteriaShape.TrimStart('\n') + "\n");
        }
        return string.Format(template, topic, LearnerLevel.Describe(level));
    }

    public GeminiSpeakingService(IGeminiClient geminiClient, FeatureFlags? flags = null)
    {
        _geminiClient = geminiClient;
        _flags = flags;
    }

    public async Task<SpeakingEvaluationResult> EvaluateAsync(
        string topic, byte[] audioBytes, string audioMimeType, string level, CancellationToken ct = default)
    {
        // Brauzer ba'zan "audio/webm;codecs=opus" kabi qo'shimcha parametr bilan
        // yuboradi — Gemini toza MIME type kutadi.
        var cleanMimeType = audioMimeType.Split(';')[0];
        var audioBase64 = Convert.ToBase64String(audioBytes);
        var fourCriteria = _flags?.SpeakingFourCriteria ?? false;
        var prompt = BuildPrompt(topic, level, fourCriteria);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = cleanMimeType, data = audioBase64 } }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        var response = await _geminiClient.SendWithFallbackAsync(requestBody, ct);
        var text = await GeminiJson.ExtractTextAsync(response, ct);

        var result = GeminiJson.DeserializeStrict<SpeakingEvaluationResult>(text);
        ScoreGuard.EnsureValid(
            ("fluency", result.Fluency), ("grammar", result.Grammar), ("vocabulary", result.Vocabulary));
        if (!fourCriteria) return result;
        // Yangi qismlar tolerant o'qiladi: yo'q/noto'g'ri bo'lsa — null, natija esa baribir qaytadi.
        var extra = SpeakingFeedbackParser.Parse(text);
        return result with { Pronunciation = extra.Pronunciation, Bands = extra.Bands, PriorityFix = extra.Fix, OverallBand = extra.OverallBand };
    }
}
