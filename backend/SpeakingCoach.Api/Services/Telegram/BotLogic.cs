using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Telegram;

public enum MenuAction
{
    None,
    Review,
    Stats,
    Settings,
    Help,
}

/// <summary>Inline tugma bosilganda keladigan ma'lumot (callback_data, ≤ 64 bayt).</summary>
public abstract record BotCallback
{
    public sealed record StartReview : BotCallback;
    public sealed record ShowAnswer(Guid CardId) : BotCallback;
    public sealed record Grade(Guid CardId, ReviewGrade Value) : BotCallback;
    public sealed record AddWord(string Word) : BotCallback;
    public sealed record SetReminder(int? Hour) : BotCallback;
    public sealed record SetLang(string Lang) : BotCallback;
    public sealed record Unlink : BotCallback;
}

/// <summary>
/// Botning sof (bazaga va tarmoqqa tegmaydigan) qoidalari — testlanadi.
/// </summary>
public static partial class BotLogic
{
    public static readonly int[] ReminderHours = { 8, 13, 18, 20, 21 };
    public static readonly TimeSpan LinkTokenLifetime = TimeSpan.FromMinutes(15);

    // ---- Callback ma'lumotini yozish va o'qish ----

    public static string Encode(BotCallback cb) => cb switch
    {
        BotCallback.StartReview => "rv:start",
        BotCallback.ShowAnswer s => $"rv:show:{s.CardId:N}",
        BotCallback.Grade g => $"rv:g:{g.CardId:N}:{(int)g.Value}",
        BotCallback.AddWord w => $"w:add:{w.Word}",
        BotCallback.SetReminder r => $"set:h:{(r.Hour is int h ? h.ToString() : "off")}",
        BotCallback.SetLang l => $"set:l:{l.Lang}",
        BotCallback.Unlink => "unlink",
        _ => throw new ArgumentOutOfRangeException(nameof(cb)),
    };

    /// <summary>Noma'lum yoki buzilgan ma'lumot — null (hech qachon istisno otmaydi).</summary>
    public static BotCallback? Decode(string? data)
    {
        if (string.IsNullOrEmpty(data) || data.Length > 64) return null;
        var p = data.Split(':');
        return p switch
        {
            ["rv", "start"] => new BotCallback.StartReview(),
            ["rv", "show", var id] when Guid.TryParseExact(id, "N", out var g) => new BotCallback.ShowAnswer(g),
            ["rv", "g", var id, var v] when Guid.TryParseExact(id, "N", out var g)
                && int.TryParse(v, out var n) && Enum.IsDefined(typeof(ReviewGrade), n) => new BotCallback.Grade(g, (ReviewGrade)n),
            ["w", "add", var w] when IsLookupWord(w) => new BotCallback.AddWord(w),
            ["set", "h", "off"] => new BotCallback.SetReminder(null),
            ["set", "h", var h] when int.TryParse(h, out var hour) && hour is >= 0 and <= 23 => new BotCallback.SetReminder(hour),
            ["set", "l", var l] when Texts.Langs.Contains(l) => new BotCallback.SetLang(l),
            ["unlink"] => new BotCallback.Unlink(),
            _ => null,
        };
    }

    // ---- Kiruvchi matn ----

    [GeneratedRegex(@"^[A-Za-z][A-Za-z'\-]{0,39}$")]
    private static partial Regex WordPattern();

    /// <summary>Bitta inglizcha so'z (izoh so'rash mumkin bo'lgan).</summary>
    public static bool IsLookupWord(string? text) => text is not null && WordPattern().IsMatch(text.Trim());

    /// <summary>"/start TOKEN" yoki "/start@BotName TOKEN" → TOKEN; token bo'lmasa — "".</summary>
    public static string? StartPayload(string? text)
    {
        if (text is null) return null;
        var t = text.Trim();
        if (!t.StartsWith("/start", StringComparison.Ordinal)) return null;
        var rest = t[6..];
        if (rest.StartsWith('@')) rest = rest.Contains(' ') ? rest[rest.IndexOf(' ')..] : "";
        return rest.Trim();
    }

    /// <summary>Menyu tugmasi (uch tilning istalganida) yoki buyruq.</summary>
    public static MenuAction ParseMenu(string? text)
    {
        var t = (text ?? "").Trim();
        var cmd = t.StartsWith('/') ? t.Split(' ', '@')[0].ToLowerInvariant() : null;
        if (cmd is "/review" || MenuLabels(MenuAction.Review).Contains(t)) return MenuAction.Review;
        if (cmd is "/stats" || MenuLabels(MenuAction.Stats).Contains(t)) return MenuAction.Stats;
        if (cmd is "/settings" || MenuLabels(MenuAction.Settings).Contains(t)) return MenuAction.Settings;
        if (cmd is "/help" || MenuLabels(MenuAction.Help).Contains(t)) return MenuAction.Help;
        return MenuAction.None;
    }

    private static IEnumerable<string> MenuLabels(MenuAction a) => Texts.Langs.Select(l => MenuLabel(l, a));

    public static string MenuLabel(string lang, MenuAction a) => Texts.Get(lang, a switch
    {
        MenuAction.Review => "bot.menu_review",
        MenuAction.Stats => "bot.menu_stats",
        MenuAction.Settings => "bot.menu_settings",
        _ => "bot.menu_help",
    });

    public static IReadOnlyList<IReadOnlyList<string>> MainMenu(string lang) => new[]
    {
        new[] { MenuLabel(lang, MenuAction.Review), MenuLabel(lang, MenuAction.Stats) },
        new[] { MenuLabel(lang, MenuAction.Settings), MenuLabel(lang, MenuAction.Help) },
    };

    /// <summary>Telegram'dagi language_code → bizning tillarimizdan biri.</summary>
    public static string LangFromTelegram(string? code) => code?.ToLowerInvariant() switch
    {
        var c when c is not null && c.StartsWith("ru") => "ru",
        var c when c is not null && c.StartsWith("uz") => "uz",
        var c when c is not null && c.StartsWith("en") => "en",
        _ => "uz",
    };

    // ---- Ulash tokeni ----

    /// <summary>Havolaga sig'adigan (≤ 64, faqat A-Za-z0-9_-) tasodifiy token.</summary>
    public static string NewLinkToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    // ---- Eslatma ----

    /// <summary>
    /// Eslatma yuborish vaqti keldimi. GitHub'ning soatlik cron'i kechikishi
    /// mumkin, shuning uchun "aynan shu soat" emas — "soat o'tgan va bugun hali
    /// yuborilmagan" (kechasi 23:00 dan keyin — endi yubormaymiz).
    /// </summary>
    public static bool ShouldRemind(int? reminderHour, DateOnly? lastSent, DateTime nowUtc, int tzOffsetMinutes)
    {
        if (reminderHour is null) return false;
        var local = nowUtc.AddMinutes(-tzOffsetMinutes);
        var today = DateOnly.FromDateTime(local);
        return local.Hour >= reminderHour && local.Hour < 23 && lastSent != today;
    }

    public static DateOnly LocalToday(DateTime nowUtc, int tzOffsetMinutes) =>
        DateOnly.FromDateTime(nowUtc.AddMinutes(-tzOffsetMinutes));

    /// <summary>Telegram HTML rejimi uchun: &lt;, &gt;, &amp; va qo'shtirnoq.</summary>
    public static string Html(string? s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>"aziza@mail.com" → "az***@mail.com" (chatda to'liq email ko'rsatilmaydi).</summary>
    public static string MaskEmail(string email) => ProgressCalculator.MaskEmail(email);
}
