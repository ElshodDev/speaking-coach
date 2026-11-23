using System.Net;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>Butun ilova uchun umumiy HTTP qoidalari: CORS, xavfsizlik sarlavhalari, javob siqish.</summary>
public class PlatformHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static HttpRequestMessage Preflight(string origin)
    {
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/profile");
        req.Headers.Add("Origin", origin);
        req.Headers.Add("Access-Control-Request-Method", "GET");
        req.Headers.Add("Access-Control-Request-Headers", "authorization");
        return req;
    }

    [Fact]
    public async Task Cors_allows_only_the_configured_frontend_and_exposes_retry_after()
    {
        var client = factory.CreateClient();
        var ok = await client.SendAsync(Preflight(ApiFactory.FrontendOrigin));
        Assert.Equal(ApiFactory.FrontendOrigin, ok.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("authorization", string.Join(",", ok.Headers.GetValues("Access-Control-Allow-Headers")), StringComparison.OrdinalIgnoreCase);

        var evil = await client.SendAsync(Preflight("https://evil.example.net"));
        Assert.False(evil.Headers.Contains("Access-Control-Allow-Origin"));

        var get = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        get.Headers.Add("Origin", ApiFactory.FrontendOrigin);
        var res = await client.SendAsync(get);
        Assert.Contains("Retry-After", string.Join(",", res.Headers.GetValues("Access-Control-Expose-Headers")));
    }

    [Fact]
    public async Task Every_response_carries_the_security_headers()
    {
        var res = await factory.CreateClient().GetAsync("/");
        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", res.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Unknown_routes_are_404_and_wrong_methods_are_405()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/does-not-exist")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task The_error_language_follows_the_accept_language_header()
    {
        var client = factory.CreateClient();
        string Error(string lang)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
            req.Headers.Add("Accept-Language", lang);
            return client.SendAsync(req).Result.JsonAsync().Result.GetProperty("error").GetString()!;
        }
        Assert.Equal(Texts.Get("ru", "login.required"), Error("ru"));
        Assert.Equal(Texts.Get("en", "login.required"), Error("en-US,en;q=0.9"));
        Assert.NotEqual(Error("ru"), Error("en"));
    }
}
