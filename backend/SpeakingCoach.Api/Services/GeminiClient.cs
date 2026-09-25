namespace SpeakingCoach.Api.Services;

/// <summary>
/// Gemini API'ga so'rov yuborishning umumiy qismi — avval Speaking uchun
/// yozilgan edi, endi Writing ham xuddi shu klassni ishlatadi (audio yoki
/// matn — Gemini'ga farqi yo'q, ikkalasi ham "contents[].parts[]" ichida
/// yuboriladi, faqat part turi farq qiladi). Bu klass FAQAT "so'rovni qayerga
/// va qanday (fallback/retry bilan) yuborish"ni biladi — PROMPT MATNINI VA
/// NATIJANI O'QISHNI BILMAYDI, bu har bir servisning (Speaking/Writing)
/// o'zining ishi. Shu ajratish — Single Responsibility Principle'ning
/// amaliy misoli: bitta klass bitta narsaga javobgar.
/// </summary>
public class GeminiClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    // Model nomi vaqt o'tishi bilan yangilanishi mumkin — agar 404 yoki
    // "model not found" xatosi chiqsa, https://ai.google.dev/gemini-api/docs/models
    // sahifasidan joriy bepul tier model nomini tekshiring.
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
    // 6-24 soniya davom etishi mumkin, shuning uchun bitta modelda uzoq
    // "tiqilib qolish" o'rniga tezroq zaxira modelga o'tish umumiy kutish
    // vaqtini qisqartiradi.
    private static readonly int[] RetryDelaysMs = { 2000 };

    public GeminiClient(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Gemini:ApiKey sozlanmagan. Lokalda: dotnet user-secrets set \"Gemini:ApiKey\" \"...\". " +
                "Railway/Render'da: Gemini__ApiKey environment variable (qo'sh pastki chiziq).");
        _http = httpClientFactory.CreateClient();
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
    public async Task<HttpResponseMessage> SendWithFallbackAsync(object requestBody, CancellationToken ct = default)
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

    /// <summary>
    /// Gemini javobidagi ichki matnni (JSON string sifatida) qazib oladi.
    /// Bu ham ikkala servisda bir xil — natijani qanday JSON'ga aylantirish
    /// (deserialize) har biriga xos, lekin Gemini javobining tashqi
    /// "qobig'i"dan matnni chiqarib olish umumiy.
    /// </summary>
    public static async Task<string> ExtractTextAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Gemini bo'sh javob qaytardi");
    }
}
