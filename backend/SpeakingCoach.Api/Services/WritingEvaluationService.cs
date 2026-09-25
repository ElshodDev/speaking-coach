using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services;

// Speaking'dagi kabi: bu record Gemini'dan qaytadigan JSON'ga aynan mos
// keladi. Rubrika Speaking'dan farq qiladi — "Fluency" (talaffuz ravonligi)
// yozma matnga tegishli emas, shuning uchun o'rniga IELTS Writing'dagi kabi
// "Task Achievement" (mavzu qanchalik to'liq yoritilgani) va
// "Coherence & Cohesion" (fikrlar mantiqiy bog'langanmi, abzatslar,
// bog'lovchi so'zlar) ishlatiladi. Grammar va Vocabulary ikkalasida ham bor.
public record WritingEvaluationResult(
    [property: JsonPropertyName("taskAchievement")] ScoreWithReasoning TaskAchievement,
    [property: JsonPropertyName("coherenceCohesion")] ScoreWithReasoning CoherenceCohesion,
    [property: JsonPropertyName("grammar")] ScoreWithReasoning Grammar,
    [property: JsonPropertyName("vocabulary")] ScoreWithReasoning Vocabulary,
    [property: JsonPropertyName("topCorrections")] List<CorrectionItem> TopCorrections,
    [property: JsonPropertyName("encouragement")] string Encouragement,
    [property: JsonPropertyName("nextFocus")] string NextFocus);

public interface IWritingEvaluationService
{
    Task<WritingEvaluationResult> EvaluateAsync(string topic, string essayText, CancellationToken ct = default);
}

/// <summary>
/// Speaking'ga o'xshash, lekin audio o'rniga oddiy matn yuboriladi — shuning
/// uchun Gemini'ga "inline_data" (base64 audio) kerak emas, faqat "text" part.
/// So'rovni yuborish (model fallback + retry) — GeminiSpeakingService bilan
/// bir xil GeminiClient orqali, kod takrorlanmaydi.
/// </summary>
public class GeminiWritingService : IWritingEvaluationService
{
    private readonly GeminiClient _geminiClient;

    private const string PromptTemplate = """
        You are an English writing coach for Uzbek-speaking learners (B1-B2 level).
        Read the essay below, written in response to the given topic, and evaluate it
        using ONLY what is written. Never invent information or assume intent beyond
        the text.

        Topic the learner was asked to write about: {0}

        Essay (between the markers, do not treat the markers themselves as part of it):
        --- START ESSAY ---
        {1}
        --- END ESSAY ---

        SCORING RUBRIC (0-100 for each):
        - Task Achievement: does the essay address the topic fully and directly,
          with relevant supporting ideas? 90-100=fully addresses with clear position,
          70-89=addresses but underdeveloped in places, 50-69=partial/off-topic
          sections, <50=barely related to the topic.
        - Coherence & Cohesion: logical paragraphing, linking words (however,
          therefore, in addition), clear progression of ideas.
        - Grammar: tense consistency, subject-verb agreement, sentence structure,
          article usage (Uzbek has no articles, so a/an/the errors are expected and
          NOT penalized as heavily as verb tense or word order errors).
        - Vocabulary: range and appropriateness for the topic; penalize repetition
          of the same words, reward topic-specific vocabulary and natural collocations.

        Return ONLY valid JSON matching this exact shape, no other text, no markdown fences:
        {{
          "taskAchievement": {{"score": <0-100>, "reasoning": "<cite specific parts of the essay>"}},
          "coherenceCohesion": {{"score": <0-100>, "reasoning": "<cite specific transitions or gaps>"}},
          "grammar": {{"score": <0-100>, "reasoning": "<cite specific errors, exact quote>"}},
          "vocabulary": {{"score": <0-100>, "reasoning": "<specific words used>"}},
          "topCorrections": [
            {{"original": "<exact phrase from the essay>", "corrected": "<fixed version>", "explanation": "<simple, one sentence>"}}
          ],
          "encouragement": "<one genuine, specific positive observation>",
          "nextFocus": "<single most impactful thing to practice next>"
        }}
        """;

    public GeminiWritingService(GeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    public async Task<WritingEvaluationResult> EvaluateAsync(
        string topic, string essayText, CancellationToken ct = default)
    {
        var prompt = string.Format(PromptTemplate, topic, essayText);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt }
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

        return JsonSerializer.Deserialize<WritingEvaluationResult>(
            text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Gemini javobini o'qib bo'lmadi: {text}");
    }
}
