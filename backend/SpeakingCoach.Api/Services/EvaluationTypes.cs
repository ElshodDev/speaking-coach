using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services;

// Bu ikki record Speaking va Writing baholashlarining IKKALASIDA ham
// ishlatiladi (masalan ikkalasida ham "Grammar: {score, reasoning}" bor,
// ikkalasida ham "eng muhim 3 ta tuzatish" ro'yxati bor). Shuning uchun
// bitta umumiy faylga chiqarilgan — SpeakingEvaluationResult va
// WritingEvaluationResult ularni qayta e'lon qilmasdan ishlatadi.

public record ScoreWithReasoning(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("reasoning")] string Reasoning);

public record CorrectionItem(
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("corrected")] string Corrected,
    [property: JsonPropertyName("explanation")] string Explanation);

public static class ScoreGuard
{
    /// <summary>
    /// JSON shakli to'g'ri bo'lsa ham, qiymat mantiqsiz bo'lishi mumkin
    /// (masalan 150 yoki -5 ball, yoki bo'sh izoh). Bunday javobni saqlashdan
    /// oldin rad etamiz. Eslatma: 0 ballning o'zi xato emas — mavzuga
    /// butunlay aloqasiz matn uchun Gemini haqiqatan 0 berishi mumkin; lekin
    /// unda ham izoh (reasoning) bo'sh bo'lmasligi kerak.
    /// </summary>
    public static void EnsureValid(params (string Name, ScoreWithReasoning Score)[] scores)
    {
        foreach (var (name, s) in scores)
        {
            if (s.Score is < 0 or > 100)
            {
                throw new InvalidOperationException($"Gemini '{name}' uchun 0-100 oralig'idan tashqari ball qaytardi: {s.Score}");
            }
            if (string.IsNullOrWhiteSpace(s.Reasoning))
            {
                throw new InvalidOperationException($"Gemini '{name}' uchun izoh (reasoning) bermadi");
            }
        }
    }
}
