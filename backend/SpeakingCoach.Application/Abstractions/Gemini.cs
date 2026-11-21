using System.Text.Json;

namespace SpeakingCoach.Application.Abstractions;

/// <summary>
/// Gemini (Google generativ AI) bilan aloqa. Application servislari faqat shu
/// interfeysni biladi; HTTP, modellar zaxirasi va qayta urinishlar —
/// Infrastructure'dagi IGeminiClient'da.
/// </summary>
public interface IGeminiClient
{
    /// <summary>So'rovni yuboradi (zaxira modellar va qisqa qayta urinish bilan) va muvaffaqiyatli javobni qaytaradi.</summary>
    Task<HttpResponseMessage> SendWithFallbackAsync(object requestBody, CancellationToken ct = default);
}

/// <summary>Gemini javobini o'qish va qat'iy JSON'ga aylantirish — toza yordamchilar (tarmoqsiz).</summary>
public static class GeminiJson
{
    /// <summary>Loglar va xato matnlarida model javobi/xato tanasidan ko'pi bilan shuncha belgi.</summary>
    public const int MaxLoggedChars = 500;

    /// <summary>Log va xato matnlari uchun: ko'pi bilan max belgi.</summary>
    public static string Truncate(string? s, int max = MaxLoggedChars)
    {
        s ??= "";
        return s.Length <= max ? s : s[..max] + "…";
    }

    /// <summary>
    /// Gemini JSON'ini C# record'larga aylantirishning QAT'IY sozlamalari.
    ///
    /// Nega kerak: standart holatda System.Text.Json yetishmayotgan maydonni
    /// jimgina standart qiymat bilan to'ldiradi — masalan Gemini "score"
    /// o'rniga boshqa nom yozsa, ball 0 bo'lib qoladi va bazaga "0 ball"
    /// deb saqlanadi (xato hech qayerda ko'rinmaydi). Bu sozlamalar bilan:
    /// - RespectRequiredConstructorParameters: record'ning har bir parametri
    ///   JSON'da bo'lishi SHART, aks holda JsonException;
    /// - RespectNullableAnnotations: `string` (nullable emas) maydonga null
    ///   kelsa ham JsonException.
    /// Natijada noto'g'ri javob bazaga yozilmaydi, foydalanuvchi xato ko'radi
    /// va qayta urinishi mumkin.
    /// </summary>
    public static readonly JsonSerializerOptions StrictJson = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    /// <summary>
    /// Gemini javob matnini T turiga qat'iy aylantiradi. Har qanday format
    /// xatosi tushunarli InvalidOperationException'ga o'giriladi (endpoint
    /// uni 502 sifatida qaytaradi).
    /// </summary>
    public static T DeserializeStrict<T>(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(text, StrictJson)
                ?? throw new InvalidOperationException($"Gemini bo'sh JSON qaytardi: {Truncate(text)}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Gemini javobi kutilgan shaklda emas ({Truncate(ex.Message, 200)}). Javob: {Truncate(text)}", ex);
        }
    }

    /// <summary>
    /// Gemini javobidagi ichki matnni (JSON string sifatida) qazib oladi.
    /// Bu ham ikkala servisda bir xil — natijani qanday JSON'ga aylantirish
    /// (deserialize) har biriga xos, lekin Gemini javobining tashqi
    /// "qobig'i"dan matnni chiqarib olish umumiy.
    /// </summary>
    public static async Task<string> ExtractTextAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        // Javob bu yerda to'liq o'qiladi — ulanish darhol bo'shatiladi.
        using var _ = response;
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Gemini bo'sh javob qaytardi");
    }
}
