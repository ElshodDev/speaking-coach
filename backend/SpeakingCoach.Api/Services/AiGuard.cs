using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Toshkent vaqti (UTC+5, yozgi vaqt yo'q). Kunlik limitlar, XP cheklovlari va
/// umumiy AI byudjeti shu kun chegarasi bo'yicha yangilanadi — foydalanuvchi
/// uchun "kun" yarim tunda boshlanadi, UTC bo'yicha soat 05:00 da emas.
/// </summary>
public static class TashkentTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(5);

    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(utcNow + Offset);

    /// <summary>Joriy Toshkent kunining boshlanishi (UTC'da).</summary>
    public static DateTime DayStartUtc(DateTime utcNow) =>
        DateTime.SpecifyKind(Today(utcNow).ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);

    public static DateTime NextDayStartUtc(DateTime utcNow) => DayStartUtc(utcNow).AddDays(1);
}

/// <summary>
/// AI vaqtincha band: umumiy byudjet tugagan, "saqlagich" (circuit breaker)
/// ochiq yoki Gemini kvotasi tugagan. Endpoint'lar buni 502 ga aylantirmaydi —
/// Program.cs'dagi markaziy middleware 503 + Retry-After qaytaradi.
/// </summary>
public sealed class AiBusyException(DateTime retryAtUtc, string reason)
    : Exception($"AI band ({reason}), {retryAtUtc:O} dan keyin urinib ko'ring")
{
    public DateTime RetryAtUtc { get; } = retryAtUtc;
    public string Reason { get; } = reason;

    public int RetryAfterSeconds(DateTime nowUtc) =>
        (int)Math.Clamp(Math.Ceiling((RetryAtUtc - nowUtc).TotalSeconds), 1, 24 * 60 * 60);
}

/// <summary>Gemini'ning qayta urinishga arzimaydigan xatosi (400, 401, 403, 404...). Validator qayta so'rashlari buni qayta yubormaydi.</summary>
public sealed class GeminiApiException(HttpStatusCode status, string message) : InvalidOperationException(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>Gemini xatosining turi — qayta urinish, zaxira model yoki to'xtash shunga qarab.</summary>
public enum GeminiFailure
{
    /// <summary>Daqiqalik chegara (429) — qisqa kutib, bir marta qayta urinish mumkin.</summary>
    RateLimited,

    /// <summary>Server band yoki vaqtinchalik xato (500/502/503/504).</summary>
    Overloaded,

    /// <summary>Kunlik (yoki model uchun nol) kvota tugagan — qayta urinish befoyda.</summary>
    QuotaExhausted,

    /// <summary>So'rov xato (400/401/403/404...) — qayta urinish o'zgartirmaydi.</summary>
    Fatal,
}

public record GeminiErrorInfo(GeminiFailure Kind, TimeSpan? RetryAfter);

/// <summary>Gemini xato javobini turlarga ajratish (toza funksiya — testlangan).</summary>
public static partial class GeminiErrors
{
    /// <summary>
    /// 429 RESOURCE_EXHAUSTED ikki xil bo'ladi: daqiqalik (PerMinute) va kunlik
    /// (PerDay) kvota. Javob tanasida QuotaFailure.quotaId va RetryInfo.retryDelay
    /// bo'ladi. Kunlik kvota, "limit: 0" (model bepul tarifda yo'q) yoki bir
    /// daqiqadan uzoq kutish — tugagan kvota deb hisoblanadi.
    /// </summary>
    public static GeminiErrorInfo Classify(HttpStatusCode status, string? body, TimeSpan? retryAfterHeader)
    {
        var text = body ?? "";
        var hint = RetryDelay(text) ?? retryAfterHeader;
        switch ((int)status)
        {
            case 429:
                var daily = text.Contains("PerDay", StringComparison.OrdinalIgnoreCase)
                    || LimitZero().IsMatch(text)
                    || hint > TimeSpan.FromSeconds(90);
                return new(daily ? GeminiFailure.QuotaExhausted : GeminiFailure.RateLimited, hint);
            case 500 or 502 or 503 or 504:
                return new(GeminiFailure.Overloaded, hint);
            default:
                return new(GeminiFailure.Fatal, null);
        }
    }

    /// <summary>"retryDelay": "37s" yoki "1.5s" — soniyalarga.</summary>
    public static TimeSpan? RetryDelay(string body)
    {
        var m = RetryDelayPattern().Match(body);
        return m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var s) && s >= 0
            ? TimeSpan.FromSeconds(Math.Min(s, 86400))
            : null;
    }

    [GeneratedRegex("\"retryDelay\"\\s*:\\s*\"(\\d+(?:\\.\\d+)?)s\"")]
    private static partial Regex RetryDelayPattern();

    [GeneratedRegex("limit:\\s*0\\b")]
    private static partial Regex LimitZero();
}

/// <summary>
/// Umumiy AI sozlamalari: Ai:GlobalPerMinute (12), Ai:GlobalPerDay (1400),
/// Ai:BreakerFailures (5), Ai:BreakerSeconds (60), Ai:QuotaCooldownMinutes (30),
/// Ai:Models (vergul bilan; birinchisi — asosiy).
/// </summary>
public class AiGuardOptions
{
    public static readonly string[] DefaultModels = ["gemini-3.6-flash", "gemini-3.5-flash-lite"];

    public int PerMinute { get; init; } = 12;
    public int PerDay { get; init; } = 1400;
    public int BreakerFailures { get; init; } = 5;
    public TimeSpan BreakerCooldown { get; init; } = TimeSpan.FromSeconds(60);
    public TimeSpan QuotaCooldown { get; init; } = TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Models { get; init; } = DefaultModels;

    public AiGuardOptions()
    {
    }

    public AiGuardOptions(IConfiguration config)
    {
        int Read(string key, int fallback) => int.TryParse(config[key], out var v) && v > 0 ? v : fallback;
        PerMinute = Read("Ai:GlobalPerMinute", 12);
        PerDay = Read("Ai:GlobalPerDay", 1400);
        BreakerFailures = Read("Ai:BreakerFailures", 5);
        BreakerCooldown = TimeSpan.FromSeconds(Read("Ai:BreakerSeconds", 60));
        QuotaCooldown = TimeSpan.FromMinutes(Read("Ai:QuotaCooldownMinutes", 30));
        Models = ParseModels(config["Ai:Models"]);
    }

    public static IReadOnlyList<string> ParseModels(string? value)
    {
        var list = (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(m => Regex.IsMatch(m, "^[A-Za-z0-9._-]{1,80}$"))
            .Distinct()
            .ToList();
        return list.Count > 0 ? list : DefaultModels;
    }
}

/// <summary>
/// Butun server bo'yicha Gemini himoyasi (bitta nusxa — singleton):
/// <list type="bullet">
/// <item>byudjet: daqiqasiga va (Toshkent) kuniga nechta so'rov — barcha foydalanuvchilar uchun umumiy;</item>
/// <item>saqlagich: ketma-ket N ta band/kvota xatosidan keyin bir muddat Gemini'ga umuman murojaat qilinmaydi;</item>
/// <item>kvotasi tugagan model bir muddat o'tkazib yuboriladi (zaxira modelga o'tiladi).</item>
/// </list>
/// Vaqt TimeProvider orqali — testda soatni boshqarish mumkin.
/// </summary>
public class AiGuard(AiGuardOptions options, TimeProvider clock)
{
    /// <summary>Daqiqalik byudjetda joy shuncha vaqtda bo'shasa — kutamiz (xato bermaymiz).</summary>
    public static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(8);

    private readonly object _lock = new();
    private readonly Queue<DateTime> _minute = new();
    private DateOnly _day;
    private int _dayCount;
    private int _failures;
    private DateTime _openUntil = DateTime.MinValue;
    private readonly Dictionary<string, DateTime> _modelBlockedUntil = new();

    public AiGuardOptions Options => options;

    public DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Bitta Gemini so'rovi uchun joy olishga urinish. false bo'lsa — retryAt'gacha
    /// kutish kerak; reason: "breaker", "minute" yoki "day".
    /// </summary>
    public bool TryAcquire(out DateTime retryAtUtc, out string reason)
    {
        var now = UtcNow;
        lock (_lock)
        {
            if (now < _openUntil)
            {
                (retryAtUtc, reason) = (_openUntil, "breaker");
                return false;
            }

            var today = TashkentTime.Today(now);
            if (today != _day)
            {
                _day = today;
                _dayCount = 0;
            }
            if (_dayCount >= options.PerDay)
            {
                (retryAtUtc, reason) = (TashkentTime.NextDayStartUtc(now), "day");
                return false;
            }

            while (_minute.Count > 0 && _minute.Peek() <= now.AddMinutes(-1)) _minute.Dequeue();
            if (_minute.Count >= options.PerMinute)
            {
                (retryAtUtc, reason) = (_minute.Peek().AddMinutes(1), "minute");
                return false;
            }

            _minute.Enqueue(now);
            _dayCount++;
            (retryAtUtc, reason) = (now, "");
            return true;
        }
    }

    /// <summary>
    /// Joy olish: daqiqalik byudjet bir necha soniyada bo'shasa — kutib turadi
    /// (bitta mashq bir nechta ketma-ket so'rov yuborishi mumkin), aks holda AiBusyException.
    /// </summary>
    public async Task AcquireAsync(CancellationToken ct)
    {
        for (var i = 0; i < 3; i++)
        {
            if (TryAcquire(out var retryAt, out var reason)) return;
            var wait = retryAt - UtcNow;
            if (reason != "minute" || wait > MaxWait) throw new AiBusyException(retryAt, reason);
            await Task.Delay(wait < TimeSpan.Zero ? TimeSpan.Zero : wait + TimeSpan.FromMilliseconds(50), ct);
        }
        TryAcquire(out var at, out var why);
        throw new AiBusyException(at, why.Length == 0 ? "minute" : why);
    }

    public void ReportSuccess()
    {
        lock (_lock) _failures = 0;
    }

    /// <summary>Band/kvota xatosi: ketma-ket N tadan keyin saqlagich ochiladi.</summary>
    public void ReportFailure()
    {
        var now = UtcNow;
        lock (_lock)
        {
            _failures++;
            if (_failures >= options.BreakerFailures) _openUntil = now + options.BreakerCooldown;
        }
    }

    public bool IsOpen(out DateTime untilUtc)
    {
        lock (_lock)
        {
            untilUtc = _openUntil;
            return UtcNow < _openUntil;
        }
    }

    /// <summary>Model kvotasi tugadi — hint (Gemini aytgan kutish) yoki sozlamadagi muddatga o'tkazib yuboriladi.</summary>
    public void MarkModelExhausted(string model, TimeSpan? hint)
    {
        var until = UtcNow + (hint is { } h && h > TimeSpan.Zero && h < options.QuotaCooldown * 4 ? h : options.QuotaCooldown);
        lock (_lock) _modelBlockedUntil[model] = until;
    }

    public bool IsModelBlocked(string model, out DateTime untilUtc)
    {
        lock (_lock)
        {
            if (_modelBlockedUntil.TryGetValue(model, out untilUtc) && UtcNow < untilUtc) return true;
            _modelBlockedUntil.Remove(model);
            return false;
        }
    }

    /// <summary>Admin sahifasi / log uchun: bugun nechta so'rov ketdi.</summary>
    public (int Today, int LastMinute, bool Open) Snapshot()
    {
        var now = UtcNow;
        lock (_lock)
        {
            return (_day == TashkentTime.Today(now) ? _dayCount : 0, _minute.Count(t => t > now.AddMinutes(-1)), now < _openUntil);
        }
    }
}
