using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpeakingCoach.Api.Services;

/// <summary>Google ID token'idan olinadigan, tekshirilgan ma'lumotlar.</summary>
public record GooglePayload(string Subject, string Email, string? Name);

/// <summary>
/// "Google bilan kirish" (Google Identity Services). Brauzer Google'dan ID
/// token (imzolangan JWT) oladi va serverga yuboradi. Server tokenga ISHONMAY,
/// o'zi tekshiradi:
///
/// 1. Imzo — Google'ning ochiq kalitlari (JWKS) bilan, RS256;
/// 2. iss — accounts.google.com; aud — AYNAN bizning Client ID (boshqa
///    saytga berilgan token bizda ishlamasin);
/// 3. exp — muddati o'tmagan;
/// 4. email_verified — Google emailni tasdiqlagan.
///
/// Tashqi NuGet paket ishlatilmaydi — .NET'ning o'z RSA'si yetarli; asosiy
/// tekshiruv sof funksiya (Validate) sifatida testlanadi.
/// </summary>
public static class GoogleJwt
{
    public static readonly string[] Issuers = { "accounts.google.com", "https://accounts.google.com" };

    /// <summary>Soatlar farqi uchun ruxsat.</summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    public static GooglePayload? Validate(
        string? token,
        IReadOnlyDictionary<string, RSAParameters> keys,
        string clientId,
        DateTime nowUtc,
        out string? error)
    {
        error = null;
        var parts = (token ?? "").Split('.');
        if (parts.Length != 3) return Fail("format", out error);

        try
        {
            using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
            var alg = header.RootElement.TryGetProperty("alg", out var a) ? a.GetString() : null;
            var kid = header.RootElement.TryGetProperty("kid", out var k) ? k.GetString() : null;
            if (alg != "RS256") return Fail("alg", out error);
            if (kid is null || !keys.TryGetValue(kid, out var key)) return Fail("kid", out error);

            using var rsa = RSA.Create();
            rsa.ImportParameters(key);
            var signed = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
            if (!rsa.VerifyData(signed, Base64UrlDecode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                return Fail("signature", out error);
            }

            using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var p = payload.RootElement;
            string? Str(string name) => p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

            if (!Issuers.Contains(Str("iss"))) return Fail("iss", out error);
            if (Str("aud") != clientId) return Fail("aud", out error);

            if (!p.TryGetProperty("exp", out var expEl) || !expEl.TryGetInt64(out var exp)) return Fail("exp", out error);
            if (DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime < nowUtc - ClockSkew) return Fail("expired", out error);

            var verified = p.TryGetProperty("email_verified", out var ev) &&
                (ev.ValueKind == JsonValueKind.True || (ev.ValueKind == JsonValueKind.String && ev.GetString() == "true"));
            var email = Str("email");
            var sub = Str("sub");
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sub)) return Fail("claims", out error);
            if (!verified) return Fail("email_unverified", out error);

            return new GooglePayload(sub, AuthService.NormalizeEmail(email), Str("name"));
        }
        catch (Exception ex) when (ex is JsonException or FormatException or CryptographicException)
        {
            return Fail("format", out error);
        }
    }

    /// <summary>Google JWKS javobidan (keys: [{kid, n, e, kty}]) RSA kalitlar.</summary>
    public static Dictionary<string, RSAParameters> ParseJwks(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new Dictionary<string, RSAParameters>();
        foreach (var k in doc.RootElement.GetProperty("keys").EnumerateArray())
        {
            if (k.GetProperty("kty").GetString() != "RSA") continue;
            result[k.GetProperty("kid").GetString()!] = new RSAParameters
            {
                Modulus = Base64UrlDecode(k.GetProperty("n").GetString()!),
                Exponent = Base64UrlDecode(k.GetProperty("e").GetString()!),
            };
        }
        return result;
    }

    public static byte[] Base64UrlDecode(string s)
    {
        var b = s.Replace('-', '+').Replace('_', '/');
        b = b.PadRight(b.Length + (4 - b.Length % 4) % 4, '=');
        return Convert.FromBase64String(b);
    }

    private static GooglePayload? Fail(string reason, out string? error)
    {
        error = reason;
        return null;
    }
}

public interface IGoogleSignIn
{
    /// <summary>Client ID sozlanganmi (sozlanmagan bo'lsa tugma ko'rsatilmaydi).</summary>
    string? ClientId { get; }

    Task<GooglePayload?> ValidateAsync(string? idToken, CancellationToken ct = default);
}

/// <summary>
/// Google'ning ochiq kalitlarini yuklab, 1 soat keshlaydi (Google ularni
/// vaqti-vaqti bilan almashtiradi — noma'lum kid kelsa, qayta yuklanadi).
/// Sozlama: Google:ClientId (Render'da Google__ClientId).
/// </summary>
public class GoogleSignInService : IGoogleSignIn
{
    private const string JwksUrl = "https://www.googleapis.com/oauth2/v3/certs";
    private static readonly TimeSpan CacheFor = TimeSpan.FromHours(1);

    private readonly HttpClient _http;
    private readonly ILogger<GoogleSignInService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, RSAParameters> _keys = new();
    private DateTime _loadedAtUtc = DateTime.MinValue;

    public GoogleSignInService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GoogleSignInService> logger)
    {
        ClientId = string.IsNullOrWhiteSpace(config["Google:ClientId"]) ? null : config["Google:ClientId"]!.Trim();
        _http = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public string? ClientId { get; }

    public async Task<GooglePayload?> ValidateAsync(string? idToken, CancellationToken ct = default)
    {
        if (ClientId is null) return null;

        var keys = await GetKeysAsync(forceReload: false, ct);
        var payload = GoogleJwt.Validate(idToken, keys, ClientId, DateTime.UtcNow, out var error);
        if (error == "kid")
        {
            // Google kalitlarni almashtirgan bo'lishi mumkin — bir marta qayta yuklaymiz.
            keys = await GetKeysAsync(forceReload: true, ct);
            payload = GoogleJwt.Validate(idToken, keys, ClientId, DateTime.UtcNow, out error);
        }
        if (payload is null) _logger.LogWarning("Google token rad etildi: {Reason}", error);
        return payload;
    }

    private async Task<Dictionary<string, RSAParameters>> GetKeysAsync(bool forceReload, CancellationToken ct)
    {
        if (!forceReload && DateTime.UtcNow - _loadedAtUtc < CacheFor && _keys.Count > 0) return _keys;
        await _lock.WaitAsync(ct);
        try
        {
            if (!forceReload && DateTime.UtcNow - _loadedAtUtc < CacheFor && _keys.Count > 0) return _keys;
            var json = await _http.GetStringAsync(JwksUrl, ct);
            _keys = GoogleJwt.ParseJwks(json);
            _loadedAtUtc = DateTime.UtcNow;
            return _keys;
        }
        finally
        {
            _lock.Release();
        }
    }
}
