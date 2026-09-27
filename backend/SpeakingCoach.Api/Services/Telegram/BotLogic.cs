using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Telegram;

public enum MenuAction
{
    None,
    Today,
    Review,
    Stats,
    Settings,
    Help,
}

/// <summary>Inline tugma bosilganda keladigan ma'lumot (callback_data, ≤ 64 bayt).</summary>
public abstract record BotCallback
{
    public sealed record StartReview : BotCallback;
    public sealed record ShowToday : BotCallback;
    public sealed record ShowAnswer(Guid CardId) : BotCallback;
    public sealed record Grade(Guid CardId, ReviewGrade Value) : BotCallback;
    public sealed record AddWord(string Word) : BotCallback;
    public sealed record SetReminder(int? Hour) : BotCallback;
    public sealed record SetLang(string Lang) : BotCallback;
    /// <summary>/lang tanlagichi (hamma uchun, hisob ulanmagan bo'lsa ham).</summary>
    public sealed record PickLang(string Lang) : BotCallback;
    public sealed record Unlink : BotCallback;

    // ---- Test qo'shish (/add) ----
    public sealed record AuthorExam(string Exam) : BotCallback;
    public sealed record AuthorKindPick(string Key) : BotCallback;
    public sealed record AuthorModePick(string Key, bool Generate) : BotCallback;
    public sealed record AuthorCancel : BotCallback;
    public sealed record TestCmd(TestAction Action, Guid Id) : BotCallback;
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
        BotCallback.ShowToday => "today",
        BotCallback.ShowAnswer s => $"rv:show:{s.CardId:N}",
        BotCallback.Grade g => $"rv:g:{g.CardId:N}:{(int)g.Value}",
        BotCallback.AddWord w => $"w:add:{w.Word}",
        BotCallback.SetReminder r => $"set:h:{(r.Hour is int h ? h.ToString() : "off")}",
        BotCallback.SetLang l => $"set:l:{l.Lang}",
        BotCallback.PickLang l => $"lang:{l.Lang}",
        BotCallback.Unlink => "unlink",
        BotCallback.AuthorExam e => $"au:e:{e.Exam}",
        BotCallback.AuthorKindPick k => $"au:k:{k.Key}",
        BotCallback.AuthorModePick m => $"au:m:{m.Key}:{(m.Generate ? "g" : "s")}",
        BotCallback.AuthorCancel => "au:x",
        BotCallback.TestCmd t => $"au:{BotAuthoring.Code(t.Action)}:{t.Id:N}",
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
            ["today"] => new BotCallback.ShowToday(),
            ["rv", "show", var id] when Guid.TryParseExact(id, "N", out var g) => new BotCallback.ShowAnswer(g),
            ["rv", "g", var id, var v] when Guid.TryParseExact(id, "N", out var g)
                && int.TryParse(v, out var n) && Enum.IsDefined(typeof(ReviewGrade), n) => new BotCallback.Grade(g, (ReviewGrade)n),
            ["w", "add", var w] when IsLookupWord(w) => new BotCallback.AddWord(w),
            ["set", "h", "off"] => new BotCallback.SetReminder(null),
            ["set", "h", var h] when int.TryParse(h, out var hour) && hour is >= 0 and <= 23 => new BotCallback.SetReminder(hour),
            ["set", "l", var l] when Texts.Langs.Contains(l) => new BotCallback.SetLang(l),
            ["lang", var l] when Texts.Langs.Contains(l) => new BotCallback.PickLang(l),
            ["unlink"] => new BotCallback.Unlink(),
            ["au", "e", var e] when BotAuthoring.Exams.Contains(e) => new BotCallback.AuthorExam(e),
            ["au", "k", var e, var m, var v] when Mock.AuthorKind.Parse($"{e}:{m}:{v}") is { } k => new BotCallback.AuthorKindPick(k.Key),
            ["au", "m", var e, var m, var v, "g" or "s"] when Mock.AuthorKind.Parse($"{e}:{m}:{v}") is { } k => new BotCallback.AuthorModePick(k.Key, p[5] == "g"),
            ["au", "x"] => new BotCallback.AuthorCancel(),
            ["au", var code, var id] when BotAuthoring.FromCode(code) is { } a && Guid.TryParseExact(id, "N", out var g) => new BotCallback.TestCmd(a, g),
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

    /// <summary>"/word" yoki "/word@BotName" (qo'shimcha matn bilan ham).</summary>
    public static bool IsCommand(string? text, string command)
    {
        var t = (text ?? "").Trim();
        return t.StartsWith('/') && t.Split(' ', '@')[0].Equals(command, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Menyu tugmasi (uch tilning istalganida) yoki buyruq.</summary>
    public static MenuAction ParseMenu(string? text)
    {
        var t = (text ?? "").Trim();
        var cmd = t.StartsWith('/') ? t.Split(' ', '@')[0].ToLowerInvariant() : null;
        if (cmd is "/today" || MenuLabels(MenuAction.Today).Contains(t)) return MenuAction.Today;
        if (cmd is "/review" || MenuLabels(MenuAction.Review).Contains(t)) return MenuAction.Review;
        if (cmd is "/stats" || MenuLabels(MenuAction.Stats).Contains(t)) return MenuAction.Stats;
        if (cmd is "/settings" || MenuLabels(MenuAction.Settings).Contains(t)) return MenuAction.Settings;
        if (cmd is "/help" || MenuLabels(MenuAction.Help).Contains(t)) return MenuAction.Help;
        return MenuAction.None;
    }

    private static IEnumerable<string> MenuLabels(MenuAction a) => Texts.Langs.Select(l => MenuLabel(l, a));

    public static string MenuLabel(string lang, MenuAction a) => Texts.Get(lang, a switch
    {
        MenuAction.Today => "bot.menu_today",
        MenuAction.Review => "bot.menu_review",
        MenuAction.Stats => "bot.menu_stats",
        MenuAction.Settings => "bot.menu_settings",
        _ => "bot.menu_help",
    });

    public static IReadOnlyList<IReadOnlyList<string>> MainMenu(string lang) => new[]
    {
        new[] { MenuLabel(lang, MenuAction.Today), MenuLabel(lang, MenuAction.Review) },
        new[] { MenuLabel(lang, MenuAction.Stats), MenuLabel(lang, MenuAction.Settings) },
        new[] { MenuLabel(lang, MenuAction.Help) },
    };

    /// <summary>Telegram'dagi language_code → bizning tillarimizdan biri.</summary>
    /// <summary>Til nomi o'z tilida, bayroq bilan (tanlagichda — har kim o'z tilini tanisin).</summary>
    public static string LangName(string lang) => lang switch
    {
        "ru" => "🇷🇺 Русский",
        "en" => "🇬🇧 English",
        _ => "🇺🇿 Oʻzbekcha",
    };

    /// <summary>Uch tilli tanlagich: joriy til ✓ bilan.</summary>
    public static IReadOnlyList<IReadOnlyList<TgButton>> LangButtons(string current) =>
        [Texts.Langs.Select(l => new TgButton((l == current ? "✓ " : "") + LangName(l), Encode(new BotCallback.PickLang(l)))).ToArray()];

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

    /// <summary>
    /// Telegram HTML rejimi uchun: faqat &amp;, &lt;, &gt; va qo'shtirnoq
    /// (Telegram talabi). WebUtility.HtmlEncode emas — u emoji va ba'zi
    /// harflarni ham &amp;#…; ga aylantirib, xabarni keraksiz uzaytiradi.
    /// </summary>
    public static string Html(string? s) =>
        (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>"aziza@mail.com" → "az***@mail.com" (chatda to'liq email ko'rsatilmaydi).</summary>
    public static string MaskEmail(string email) => ProgressCalculator.MaskEmail(email);
}
