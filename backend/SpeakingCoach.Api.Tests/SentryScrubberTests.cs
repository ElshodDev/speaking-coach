using Sentry;

public class SentryScrubberTests
{
    [Fact]
    public void Text_masks_emails_tokens_secrets_and_cuts_model_output()
    {
        Assert.Equal("Kirish: [email] topilmadi", SentryScrubber.ScrubText("Kirish: aziza.k@mail.uz topilmadi"));
        Assert.Equal("Bearer [token]", SentryScrubber.ScrubText("Bearer abcdef1234567890XYZ"));
        Assert.Contains("[token]", SentryScrubber.ScrubText($"hash {new string('a', 64)}"));
        Assert.Equal("Host=db;Password=[secret]", SentryScrubber.ScrubText("Host=db;Password=hunter2"));
        // Gemini noto'g'ri shakl qaytarganda xato matni "Javob: <nutq>" bilan tugaydi — foydalanuvchi nutqi chiqib ketmasin.
        var cut = SentryScrubber.ScrubText("Gemini javobi kutilgan shaklda emas (x). Javob: {\"transcript\":\"My name is Aziza and I live in Tashkent\"}");
        Assert.DoesNotContain("Aziza", cut);
        Assert.DoesNotContain("Tashkent", cut);
        Assert.Contains("Javob: [cut]", cut);
        Assert.True(SentryScrubber.ScrubText(new string('x', 5000)).Length <= SentryScrubber.MaxMessageChars + 1);
    }

    [Fact]
    public void Url_loses_query_and_fragment()
    {
        Assert.Equal("https://api.x.com/api/auth/login", SentryScrubber.ScrubUrl("https://api.x.com/api/auth/login?email=a@b.uz#x"));
        Assert.Equal("/api/review/due", SentryScrubber.ScrubUrl("/api/review/due"));
        Assert.Null(SentryScrubber.ScrubUrl(null));
    }

    [Fact]
    public void Event_loses_user_body_headers_cookies_query_extra_and_secrets_in_messages()
    {
        var evt = new SentryEvent(new InvalidOperationException("Failed for a@b.uz with Bearer abcdef1234567890XYZ"))
        {
            User = new SentryUser { Id = "1", Email = "a@b.uz", IpAddress = "1.2.3.4", Username = "aziza" },
            ServerName = "render-host",
            Message = new SentryMessage { Message = "Xato: a@b.uz" },
        };
        evt.Request.Url = "https://api.fluentuz/api/speaking/submit?topic=secret";
        evt.Request.Data = "<8MB of audio bytes>";
        evt.Request.Cookies = "session=abc";
        evt.Request.QueryString = "topic=secret";
        evt.Request.Headers["Authorization"] = "Bearer abcdef1234567890XYZ";
        evt.Request.Env["REMOTE_ADDR"] = "1.2.3.4";
        evt.SetExtra("transcript", "My name is Aziza");

        SentryScrubber.Scrub(evt);

        Assert.Null(evt.User.Email);
        Assert.Null(evt.User.IpAddress);
        Assert.Null(evt.User.Username);
        Assert.Null(evt.User.Id);
        Assert.Null(evt.ServerName);
        Assert.Null(evt.Request.Data);
        Assert.Null(evt.Request.Cookies);
        Assert.Null(evt.Request.QueryString);
        Assert.Empty(evt.Request.Headers);
        Assert.Empty(evt.Request.Env);
        Assert.Equal("https://api.fluentuz/api/speaking/submit", evt.Request.Url);
        Assert.Equal("Xato: [email]", evt.Message!.Message);
        Assert.All(evt.Extra.Values, v => Assert.Null(v));
        var text = string.Join(" ", evt.SentryExceptions!.Select(e => e.Value));
        Assert.DoesNotContain("a@b.uz", text);
        Assert.DoesNotContain("abcdef1234567890", text);
    }
}
