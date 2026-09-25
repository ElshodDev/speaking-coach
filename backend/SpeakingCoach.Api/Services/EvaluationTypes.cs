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
