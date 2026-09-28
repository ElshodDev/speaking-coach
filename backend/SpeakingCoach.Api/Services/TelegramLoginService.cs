using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

public enum TelegramLoginStatus
{
    Pending,
    Expired,
    Rejected,
    Confirmed,
}

/// <summary>
/// "Telegram orqali kirish" qoidalari — bazaga va tarmoqqa tegmaydigan sof
/// funksiyalar (testlanadi). Oqim:
/// <list type="number">
/// <item>sayt: POST /api/auth/telegram/start → bot havolasi (t.me/Bot?start=login_NONCE), 2 xonali kod va poll token;</item>
/// <item>bot: /start login_NONCE → urinish shu chatga bog'lanadi, 3 ta raqamli tugma (bittasi — saytdagi kod);</item>
/// <item>to'g'ri raqam → tasdiqlandi (hisob topiladi yoki ochiladi); noto'g'ri yoki "Bu men emasman" → bekor;</item>
/// <item>sayt: POST /api/auth/telegram/poll → bir marta sessiya beriladi.</item>
/// </list>
/// Nonce va poll token bazada faqat SHA-256 xeshi sifatida saqlanadi.
/// </summary>
public static partial class TelegramLoginRules
{
    /// <summary>Urinish muddati: shu vaqt ichida botda tasdiqlash kerak.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    /// <summary>Tasdiqlangandan keyin sayt sessiyani shu vaqt ichida olishi kerak.</summary>
    public static readonly TimeSpan ConsumeWindow = TimeSpan.FromMinutes(5);

    /// <summary>Eski urinishlar (1 kundan eski) tozalanadi.</summary>
    public static readonly TimeSpan Keep = TimeSpan.FromDays(1);

    /// <summary>/start parametri prefiksi: "login_" + nonce (jami ≤ 64, faqat A-Za-z0-9_-).</summary>
    public const string StartPrefix = "login_";

    /// <summary>Sintetik (xat yuborib bo'lmaydigan) emaillar domeni — RFC 2606 ".invalid".</summary>
    public const string SyntheticDomain = "telegram.invalid";

    /// <summary>Telegram orqali ochilgan hisobning emaili: "tg-123456@telegram.invalid".</summary>
    public static string SyntheticEmail(long telegramUserId) =>
        $"tg-{telegramUserId.ToString(System.Globalization.CultureInfo.InvariantCulture)}@{SyntheticDomain}";

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>24 bayt tasodifiy → 32 belgi base64url ("login_" bilan 38 ≤ 64).</summary>
    public static string NewNonce() => Base64Url(RandomNumberGenerator.GetBytes(24));

    /// <summary>32 bayt tasodifiy → 43 belgi base64url.</summary>
    public static string NewPollToken() => Base64Url(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>"10"–"99", kriptografik tasodifiy.</summary>
    public static string NewCode() => RandomNumberGenerator.GetInt32(10, 100).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static bool IsValidCode(int n) => n is >= 10 and <= 99;

    /// <summary>/start parametri → nonce (login_ bilan boshlanmasa yoki buzilgan bo'lsa — null).</summary>
    public static string? NonceFromStart(string? startPayload)
    {
        if (startPayload is null || !startPayload.StartsWith(StartPrefix, StringComparison.Ordinal)) return null;
        var nonce = startPayload[StartPrefix.Length..];
        return nonce.Length is >= 16 and <= 58 && NonceChars().IsMatch(nonce) ? nonce : null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex NonceChars();

    /// <summary>
    /// Botdagi 3 ta raqam: haqiqiy kod + 2 ta boshqa (bir-biridan farqli)
    /// tasodifiy raqam, aralashtirilgan. next(min, maxExclusive) — tasodif
    /// manbai (testda — aniq ketma-ketlik).
    /// </summary>
    public static IReadOnlyList<string> Choices(string code, Func<int, int, int>? next = null)
    {
        next ??= RandomNumberGenerator.GetInt32;
        var set = new List<string> { code };
        var guard = 0;
        while (set.Count < 3 && guard++ < 1000)
        {
            var d = next(10, 100).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!set.Contains(d)) set.Add(d);
        }
        // Zaxira: tasodif manbai bir xil raqam qaytaraversa ham 3 ta farqli raqam bo'lsin.
        for (var n = 10; set.Count < 3; n++)
        {
            var d = n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!set.Contains(d)) set.Add(d);
        }
        // Fisher–Yates.
        for (var i = set.Count - 1; i > 0; i--)
        {
            var j = next(0, i + 1);
            (set[i], set[j]) = (set[j], set[i]);
        }
        return set;
    }

    /// <summary>Tugmadagi raqam saytdagi kodga tengmi.</summary>
    /// <summary>Botga yozilgan xabar — 2 xonali kod ("47", " 47 ")? Boshqa hech narsa emas.</summary>
    public static bool IsTypedCode(string? text) =>
        text is not null && text.Trim() is { Length: 2 } t && t[0] is >= '1' and <= '9' && t[1] is >= '0' and <= '9';

    public static bool Matches(string code, int? number) =>
        number is int n && IsValidCode(n) && code == n.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Botda hali tasdiqlash/bekor qilish mumkinmi.</summary>
    public static bool IsOpen(TelegramLogin login, DateTime nowUtc) =>
        login.ConfirmedAtUtc is null && login.RejectedAtUtc is null && login.ConsumedAtUtc is null && login.ExpiresAtUtc > nowUtc;

    /// <summary>Saytning poll so'roviga javob holati.</summary>
    public static TelegramLoginStatus StatusOf(TelegramLogin login, DateTime nowUtc)
    {
        if (login.RejectedAtUtc is not null) return TelegramLoginStatus.Rejected;
        if (login.ConsumedAtUtc is not null) return TelegramLoginStatus.Expired;
        if (login.ConfirmedAtUtc is { } confirmed)
        {
            if (nowUtc - confirmed > ConsumeWindow) return TelegramLoginStatus.Expired;
            // Tasdiqlandi, hisob hali ochilmoqda (bir necha millisoniya) — kutamiz.
            return login.UserId is null ? TelegramLoginStatus.Pending : TelegramLoginStatus.Confirmed;
        }
        return login.ExpiresAtUtc > nowUtc ? TelegramLoginStatus.Pending : TelegramLoginStatus.Expired;
    }

    public static string StatusName(TelegramLoginStatus s) => s switch
    {
        TelegramLoginStatus.Pending => "pending",
        TelegramLoginStatus.Rejected => "rejected",
        TelegramLoginStatus.Confirmed => "confirmed",
        _ => "expired",
    };

    // Profil sahifasidagi taxallus qoidasi bilan bir xil (2–30: harf, raqam, bo'sh joy, . _ ' -).
    [GeneratedRegex(@"^[\p{L}\p{N} ._'\-]{2,30}$")]
    private static partial Regex DisplayNamePattern();

    /// <summary>
    /// Telegram ismi → taxallus: bo'sh joylar qisqartiriladi, 30 belgiga
    /// (baza cheklovi) qisqartiriladi; qoidaga to'g'ri kelmasa (emoji va h.k.) — null.
    /// </summary>
    public static string? CleanDisplayName(string? firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName)) return null;
        var name = string.Join(' ', firstName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (name.Length > 30) name = name[..30].TrimEnd();
        return DisplayNamePattern().IsMatch(name) ? name : null;
    }

    /// <summary>Saqlash uchun qisqartirish (null/bo'sh — null).</summary>
    public static string? Clip(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length > max ? t[..max] : t;
    }

    public static string NormalizeLang(string? lang) => Texts.Langs.Contains(lang) ? lang! : Texts.DefaultLang;
}
