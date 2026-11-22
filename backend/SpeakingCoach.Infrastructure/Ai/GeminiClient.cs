using System.Text.Json;

namespace SpeakingCoach.Infrastructure.Ai;

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
public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly AiGuard _guard;
    private readonly ILogger<GeminiClient> _logger;

    // Model nomlari sozlanadi (Ai:Models, vergul bilan): birinchisi — asosiy,
    // qolganlari — zaxira. Eng yangi flash model talab yuqori bo'lganda 503
    // "high demand" qaytarishi ma'lum muammo — u band bo'lsa, yengilroq va
    // odatda ko'proq bo'sh sig'imga ega keyingi modelga avtomatik o'tamiz.
    // 404 yoki "model not found" chiqsa — https://ai.google.dev/gemini-api/docs/models
    // sahifasidan joriy bepul tier model nomini tekshiring.
    private IReadOnlyList<string> Models => _guard.Options.Models;

    /// <summary>Vaqtinchalik xatoda (429 daqiqalik / 5xx) bitta model uchun faqat 1 marta qayta urinamiz.</summary>
    public static readonly TimeSpan DefaultBackoff = TimeSpan.FromMilliseconds(1500);

    /// <summary>Gemini "retryDelay" (yoki Retry-After) shundan uzoq bo'lsa — shu modelda kutmaymiz.</summary>
    public static readonly TimeSpan MaxHonoredDelay = TimeSpan.FromSeconds(5);

    /// <summary>Loglar va xato matnlarida model javobi/xato tanasidan ko'pi bilan shuncha belgi.</summary>
    public const int MaxLoggedChars = GeminiJson.MaxLoggedChars;

    public GeminiClient(IConfiguration config, IHttpClientFactory httpClientFactory, AiGuard guard, ILogger<GeminiClient> logger)
    {
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Gemini:ApiKey sozlanmagan. Lokalda: dotnet user-secrets set \"Gemini:ApiKey\" \"...\". " +
                "Railway/Render'da: Gemini__ApiKey environment variable (qo'sh pastki chiziq).");
        // Faqat lokal/e2e test uchun: soxta (stub) Gemini serveriga yo'naltirish. Production'da bo'sh qoldiriladi.
        _baseUrl = (config["Gemini:BaseUrl"] is { Length: > 0 } b ? b : "https://generativelanguage.googleapis.com").TrimEnd('/');
        _guard = guard;
        _logger = logger;
        _http = httpClientFactory.CreateClient();
        // Butun testni (masalan PDF'dan Reading) o'girish 100 soniyadan uzoq
        // davom etishi mumkin; odatiy so'rovlar baribir ancha tez tugaydi.
        _http.Timeout = TimeSpan.FromMinutes(3);
    }

    /// <summary>
    /// So'rovni yuboradi: har bir model uchun birinchi urinish + vaqtinchalik
    /// xatoda (429 daqiqalik yoki 5xx) ko'pi bilan 1 ta qayta urinish (qisqa
    /// kutib; Gemini aytgan kutish 5 soniyadan oshsa — kutmasdan keyingi modelga).
    /// Kunlik kvota tugagan bo'lsa — qayta urinilmaydi, model bir muddat
    /// o'tkazib yuboriladi. Boshqa xatolar (400, 401/403...) darhol otiladi.
    /// Har bir HTTP urinish umumiy byudjetdan (AiGuard) joy oladi; byudjet
    /// tugagan yoki saqlagich ochiq bo'lsa — AiBusyException (503).
    /// </summary>
    public async Task<HttpResponseMessage> SendWithFallbackAsync(object requestBody, CancellationToken ct = default)
    {
        // Saqlagich ochiq bo'lsa — hatto JSON'ni ham yozib o'tirmaymiz.
        if (_guard.IsOpen(out var openUntil)) throw new AiBusyException(openUntil, "breaker");

        // JSON bir marta yoziladi (audio base64 bilan bir necha MB bo'lishi mumkin) —
        // har bir urinish/model uchun qayta serializatsiya qilinmaydi.
        var body = JsonSerializer.SerializeToUtf8Bytes(requestBody, requestBody.GetType(), RequestJson);

        // Katta so'rovlar (audio, PDF) bir vaqtda ko'pi bilan ikkitadan —
        // server xotirasi (Render: 512 MB) to'lib qolmasin.
        var big = body.Length > BigPayloadBytes;
        if (big) await BigPayloads.WaitAsync(ct);
        try
        {
            return await SendCoreAsync(body, ct);
        }
        finally
        {
            if (big) BigPayloads.Release();
        }
    }

    // PostAsJsonAsync bilan bir xil (camelCase) — so'rov shakli o'zgarmaydi.
    private static readonly JsonSerializerOptions RequestJson = new(JsonSerializerDefaults.Web);

    private const int BigPayloadBytes = 2 * 1024 * 1024;
    private static readonly SemaphoreSlim BigPayloads = new(2, 2);

    private async Task<HttpResponseMessage> SendCoreAsync(byte[] body, CancellationToken ct)
    {
        System.Net.HttpStatusCode? lastStatus = null;
        GeminiErrorInfo? lastFailure = null;
        DateTime? earliestUnblock = null;
        GeminiApiException? notFound = null;

        foreach (var model in Models)
        {
            if (_guard.IsModelBlocked(model, out var until))
            {
                earliestUnblock = earliestUnblock is null || until < earliestUnblock ? until : earliestUnblock;
                continue;
            }

            // Kalit URL'da emas, sarlavhada: URL'lar loglarga (HttpClient,
            // proksi) yozilib qolishi mumkin.
            var url = $"{_baseUrl}/v1beta/models/{model}:generateContent";

            for (var attempt = 0; attempt < 2; attempt++)
            {
                await _guard.AcquireAsync(ct);

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("x-goog-api-key", _apiKey);
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                HttpResponseMessage response;
                try
                {
                    response = await _http.SendAsync(request, ct);
                }
                catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
                {
                    // Tarmoq xatosi yoki 3 daqiqalik timeout — Gemini "band" deb hisoblanadi.
                    _guard.ReportFailure();
                    throw;
                }

                if (response.IsSuccessStatusCode)
                {
                    _guard.ReportSuccess();
                    return response;
                }

                TimeSpan? retryAfter;
                string errorBody;
                using (response)
                {
                    // Turini aniqlash uchun to'liq tana kerak (quotaId, retryDelay oxirida keladi); logga — qisqasi.
                    errorBody = GeminiJson.Truncate(await response.Content.ReadAsStringAsync(ct), 16_000);
                    lastStatus = response.StatusCode;
                    retryAfter = response.Headers.RetryAfter?.Delta;
                }
                var lastErrorBody = GeminiJson.Truncate(errorBody, MaxLoggedChars);
                var failure = GeminiErrors.Classify(lastStatus.Value, errorBody, retryAfter);
                lastFailure = failure;

                if (failure.Kind == GeminiFailure.Fatal)
                {
                    var fatal = new GeminiApiException(lastStatus.Value, $"Gemini API xatosi ({lastStatus}, {model}): {lastErrorBody}");
                    // Model nomi eskirgan (404) — keyingi modelni sinaymiz; boshqa xatolar so'rovning o'zida.
                    if (lastStatus != System.Net.HttpStatusCode.NotFound) throw fatal;
                    _logger.LogError("Gemini modeli topilmadi: {Model} — Ai:Models sozlamasini tekshiring", model);
                    notFound = fatal;
                    break;
                }

                _guard.ReportFailure();
                _logger.LogWarning("Gemini {Model}: {Status} ({Kind})", model, (int)lastStatus.Value, failure.Kind);

                if (failure.Kind == GeminiFailure.QuotaExhausted)
                {
                    // Kunlik kvota — qayta urinish befoyda: model bir muddat chetga.
                    _guard.MarkModelExhausted(model, failure.RetryAfter);
                    _guard.IsModelBlocked(model, out var blockedUntil);
                    earliestUnblock = earliestUnblock is null || blockedUntil < earliestUnblock ? blockedUntil : earliestUnblock;
                    break;
                }

                // Vaqtinchalik xato: bir marta, qisqa kutib. Uzoq kutish so'ralsa — keyingi modelga.
                var delay = failure.RetryAfter ?? DefaultBackoff;
                if (attempt > 0 || delay > MaxHonoredDelay) break;
                await Task.Delay(delay, ct);
            }
            // Shu model uchun urinishlar tugadi — keyingi (zaxira) modelga o'tamiz.
        }

        // Hamma model band yoki kvotasi tugagan — bu "keyinroq urinib ko'ring" holati (503).
        var now = _guard.UtcNow;
        if (lastFailure?.Kind == GeminiFailure.Fatal && notFound is not null) throw notFound;
        if (lastFailure is null || lastFailure.Kind == GeminiFailure.QuotaExhausted)
        {
            throw new AiBusyException(earliestUnblock ?? now.AddMinutes(1), "quota");
        }
        var wait = lastFailure.RetryAfter is { } h && h > TimeSpan.FromSeconds(10) ? h : TimeSpan.FromSeconds(30);
        throw new AiBusyException(now + (wait > TimeSpan.FromMinutes(10) ? TimeSpan.FromMinutes(10) : wait),
            $"overloaded {(int?)lastStatus}");
    }
}
