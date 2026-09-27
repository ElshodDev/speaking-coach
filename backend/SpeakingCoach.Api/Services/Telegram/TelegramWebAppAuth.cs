using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// Mini App (sayt Telegram ichida) ochilganda Telegram sahifaga "initData"
/// beradi — imzolangan qator. Uni bot tokeni bilan tekshiramiz (Telegram
/// hujjati: "Validating data received via the Mini App"):
///   secret = HMAC_SHA256(key: "WebAppData", data: bot_token)
///   hash   = hex(HMAC_SHA256(key: secret, data: data_check_string))
/// data_check_string — "hash" dan boshqa barcha maydonlar "key=value"
/// ko'rinishida, alifbo tartibida, "\n" bilan. Imzo to'g'ri va yangi
/// (1 soat ichida — begona havola bilan qayta ishlatib bo'lmasin) bo'lsa — Telegram foydalanuvchisi haqiqiy.
/// </summary>
public static class TelegramWebAppAuth
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    public static string DataCheckString(IEnumerable<KeyValuePair<string, string>> fields) =>
        string.Join("\n", fields.Where(f => f.Key != "hash").OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => $"{f.Key}={f.Value}"));

    public static string Sign(string dataCheckString, string botToken)
    {
        var secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        return Convert.ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(dataCheckString))).ToLowerInvariant();
    }

    /// <summary>initData ("query string") → maydonlar; buzilgan bo'lsa — null.</summary>
    public static Dictionary<string, string>? Parse(string? initData)
    {
        if (string.IsNullOrWhiteSpace(initData) || initData.Length > 8192) return null;
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            if (i <= 0) return null;
            var key = Uri.UnescapeDataString(part[..i].Replace('+', ' '));
            var value = Uri.UnescapeDataString(part[(i + 1)..].Replace('+', ' '));
            if (!result.TryAdd(key, value)) return null;
        }
        return result;
    }

    /// <summary>Tekshirilgan Telegram foydalanuvchisi (ID) yoki null: imzo noto'g'ri, eskirgan yoki user yo'q.</summary>
    public static long? Validate(string? initData, string? botToken, DateTime nowUtc)
    {
        if (botToken is null) return null;
        var fields = Parse(initData);
        if (fields is null || !fields.TryGetValue("hash", out var hash) || hash.Length != 64) return null;

        var expected = Sign(DataCheckString(fields), botToken);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(hash.ToLowerInvariant()))) return null;

        if (!fields.TryGetValue("auth_date", out var ad) || !long.TryParse(ad, out var unix)) return null;
        var authDate = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        if (nowUtc - authDate > MaxAge || authDate - nowUtc > TimeSpan.FromMinutes(5)) return null;

        if (!fields.TryGetValue("user", out var userJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(userJson);
            return doc.RootElement.TryGetProperty("id", out var id) && id.TryGetInt64(out var v) ? v : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
