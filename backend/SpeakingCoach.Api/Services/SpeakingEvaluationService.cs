using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services;

// Bu record'lar Gemini'dan qaytadigan JSON'ga aynan mos keladi.
// Property nomini o'zgartirsangiz, PromptTemplate'dagi OUTPUT FORMAT
// qismini ham yangilang — ikkalasi sinxron turishi kerak.

public record ScoreWithReasoning(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("reasoning")] string Reasoning);

public record CorrectionItem(
    [property: JsonPropertyName("original")] string Original,
    [property: JsonPropertyName("corrected")] string Corrected,
    [property: JsonPropertyName("explanation")] string Explanation);

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
/// </summary>
public class GeminiSpeakingService : ISpeakingEvaluationService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    // Model nomi vaqt o'tishi bilan yangilanishi mumkin — agar 404 yoki
    // "model not found" xatosi chiqsa, https://ai.google.dev/gemini-api/docs/models
    // sahifasidan joriy bepul tier model nomini tekshiring. (gemini-2.0-flash
    // 2026 yil davomida to'xtatilgan, gemini-3.6-flash'ga almashtirildi —
    // buni Gemini API'ning o'z xato xabaridan bilib oldik.)
    private const string Model = "gemini-3.6-flash";

    // Eng yangi/kuchli flash model (yuqoridagi Model) talab yuqori bo'lganda
    // 503 "high demand" qaytarishi ma'lum muammo — hatto pullik tarifda ham
    // xabar berilgan. Shuning uchun u band bo'lsa, yengilroq va odatda
    // ko'proq bo'sh sig'imga ega FallbackModel'ga avtomatik o'tamiz
    // (natija sifati bir oz farq qilishi mumkin, lekin funksiya ishlab turadi).
    private const string FallbackModel = "gemini-3.5-flash-lite";
    private static readonly string[] ModelsInPriorityOrder = { Model, FallbackModel };

    // Har bir model uchun faqat 1 marta qayta urinamiz (2s kutib), keyin
    // darhol keyingi modelga o'tamiz — chunki 503 javobining o'zi ham
    // 6-24 soniya davom etishi mumkin (yuqoridagi log'da ko'ringandek),
    // shuning uchun bitta modelda uzoq "tiqilib qolish" o'rniga tezroq
    // zaxira modelga o'tish umumiy kutish vaqtini qisqartiradi.
    private static readonly int[] RetryDelaysMs = { 2000 };

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

    public GeminiSpeakingService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Gemini:ApiKey sozlanmagan. Lokalda: dotnet user-secrets set \"Gemini:ApiKey\" \"...\". " +
                "Railway/Render'da: Gemini__ApiKey environment variable (qo'sh pastki chiziq).");
        _http = httpClientFactory.CreateClient();
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

        var response = await SendToGeminiWithFallbackAsync(requestBody, ct);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Gemini bo'sh javob qaytardi");

        return JsonSerializer.Deserialize<SpeakingEvaluationResult>(
            text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Gemini javobini o'qib bo'lmadi: {text}");
    }

    /// <summary>
    /// Har bir model uchun: birinchi urinish + RetryDelaysMs.Length ta qayta
    /// urinish (orada kutib). Faqat 503 (ServiceUnavailable) va 429
    /// (TooManyRequests) qayta urinishga arziydi — boshqa xatolar (masalan
    /// 400 — noto'g'ri so'rov, 401/403 — noto'g'ri API kalit) qayta
    /// urinsangiz ham o'zgarmaydi, shuning uchun darhol otiladi.
    /// Model retrylari tugab, hali ham band bo'lsa — ModelsInPriorityOrder
    /// dagi keyingi (yengilroq) modelga o'tamiz.
    /// </summary>
    private async Task<HttpResponseMessage> SendToGeminiWithFallbackAsync(object requestBody, CancellationToken ct)
    {
        string? lastErrorBody = null;
        System.Net.HttpStatusCode? lastStatus = null;

        foreach (var model in ModelsInPriorityOrder)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

            for (var attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
            {
                var response = await _http.PostAsJsonAsync(url, requestBody, ct);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                lastErrorBody = await response.Content.ReadAsStringAsync(ct);
                lastStatus = response.StatusCode;
                var isRetryable = response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                    || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests;

                if (!isRetryable)
                {
                    throw new InvalidOperationException($"Gemini API xatosi ({response.StatusCode}): {lastErrorBody}");
                }

                if (attempt < RetryDelaysMs.Length)
                {
                    await Task.Delay(RetryDelaysMs[attempt], ct);
                }
            }
            // Shu model uchun barcha urinishlar tugadi (baribir 503/429) —
            // tashqi foreach ModelsInPriorityOrder'dagi keyingi modelga o'tadi.
        }

        throw new InvalidOperationException(
            $"Barcha modellar band ({lastStatus}), {ModelsInPriorityOrder.Length} ta model, "
            + $"har biri {RetryDelaysMs.Length + 1} marta sinaldi. Oxirgi xato: {lastErrorBody}");
    }
}
