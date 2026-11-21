using System.Text.RegularExpressions;
using Sentry;

namespace SpeakingCoach.Api.Web;

/// <summary>
/// Sentry'ga ketadigan hodisalarni tozalash. Maqsad: ovoz, matn (transkript, insho), foydalanuvchi aloqa ma'lumoti va
/// tokenlar hech qachon tashqariga chiqmasin. So'rov tanasi, sarlavhalar, cookie, so'rov qismi (query), foydalanuvchi va
/// qo'shimcha ma'lumotlar butunlay olib tashlanadi; xato matnlaridagi email/token niqoblanadi va Gemini javobidan
/// parcha ("Javob: ...") kesib tashlanadi (u foydalanuvchi nutqini o'z ichiga olishi mumkin).
/// </summary>
public static partial class SentryScrubber
{
    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
    private static partial Regex Email();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9._~+/=\-]{8,}", RegexOptions.IgnoreCase)]
    private static partial Regex Bearer();

    [GeneratedRegex(@"\b[A-Fa-f0-9]{32,}\b")]
    private static partial Regex LongToken();

    /// <summary>Gemini xatolari "... Javob: &lt;model matni&gt;" bilan tugaydi — o'sha qism kesiladi.</summary>
    [GeneratedRegex(@"(Javob|Response|Raw)\s*:.*", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ModelOutput();

    /// <summary>Connection string / parol qismlari.</summary>
    [GeneratedRegex(@"(Password|Pwd|ApiKey|api[_-]?key|token|secret)\s*=\s*[^;\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex Secret();

    public const int MaxMessageChars = 300;

    public static string ScrubText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        var s = ModelOutput().Replace(text, "$1: [cut]");
        s = Secret().Replace(s, "$1=[secret]");
        s = Email().Replace(s, "[email]");
        s = Bearer().Replace(s, "Bearer [token]");
        s = LongToken().Replace(s, "[token]");
        return s.Length > MaxMessageChars ? s[..MaxMessageChars] + "…" : s;
    }

    public static string? ScrubUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        var cut = url.IndexOfAny(['?', '#']);
        return cut < 0 ? url : url[..cut];
    }

    /// <summary>Hodisani tozalaydi; Sentry SDK'dagi BeforeSend shuni chaqiradi.</summary>
    public static SentryEvent Scrub(SentryEvent evt)
    {
        evt.User = new SentryUser();
        evt.ServerName = null;

        var req = evt.Request;
        req.Data = null;
        req.Cookies = null;
        req.QueryString = null;
        req.Headers.Clear();
        req.Env.Clear();
        req.Url = ScrubUrl(req.Url);

        if (evt.Message is { } m)
        {
            m.Message = ScrubText(m.Message);
            m.Formatted = ScrubText(m.Formatted);
            m.Params = null;
        }
        foreach (var ex in evt.SentryExceptions ?? [])
        {
            ex.Value = ScrubText(ex.Value);
        }
        foreach (var key in evt.Extra.Keys.ToList())
        {
            evt.SetExtra(key, null);
        }
        return evt;
    }
}
