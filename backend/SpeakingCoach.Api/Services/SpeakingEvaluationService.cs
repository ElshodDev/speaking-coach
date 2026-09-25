using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services;

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
    [property: JsonPropertyName("nextFocus")] string NextFocus);

public interface ISpeakingEvaluationService
{
    Task<SpeakingEvaluationResult> EvaluateAsync(
        string topic, byte[] audioBytes, string audioMimeType, CancellationToken ct = default);
}

/// <summary>
/// Gemini multimodal model — audio faylni to'g'ridan-to'g'ri qabul qiladi,
/// shuning uchun alohida Speech-to-Text (Whisper) qatlami kerak emas:
/// transkripsiya va baholash bitta so'rovda amalga oshadi.
/// Google AI Studio (aistudio.google.com/apikey) bepul API kalit beradi,
/// kredit karta talab qilmaydi — shuning uchun bu yerda tanlangan.
///
/// So'rovni Gemini'ga yuborish (model fallback + retry) endi GeminiClient
/// ichida — bu klass faqat PROMPT MATNI va NATIJANI O'QISH bilan shug'ullanadi
/// (Writing bilan solishtiring: WritingEvaluationService.cs xuddi shu
/// GeminiClient'ni ishlatadi, faqat boshqa prompt va boshqa natija shakli
/// bilan).
/// </summary>
public class GeminiSpeakingService : ISpeakingEvaluationService
{
    private readonly GeminiClient _geminiClient;

    private const string PromptTemplate = """
        You are an English speaking coach for Uzbek-speaking learners (B1-B2 level).
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

    public GeminiSpeakingService(GeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public async Task<SpeakingEvaluationResult> EvaluateAsync(
        string topic, byte[] audioBytes, string audioMimeType, CancellationToken ct = default)
    {
        // Brauzer ba'zan "audio/webm;codecs=opus" kabi qo'shimcha parametr bilan
        // yuboradi — Gemini toza MIME type kutadi.
        var cleanMimeType = audioMimeType.Split(';')[0];
        var audioBase64 = Convert.ToBase64String(audioBytes);
        var prompt = string.Format(PromptTemplate, topic);

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
        var text = await GeminiClient.ExtractTextAsync(response, ct);

        return JsonSerializer.Deserialize<SpeakingEvaluationResult>(
            text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Gemini javobini o'qib bo'lmadi: {text}");
    }
}
