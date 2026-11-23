using System.Net;
namespace SpeakingCoach.Api.Tests.Integration;

public class SmokeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Root_and_health_endpoints_answer()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("ok", (await health.JsonAsync()).GetProperty("database").GetString());
    }

    [Fact]
    public async Task A_logged_in_user_can_read_their_profile()
    {
        var user = await factory.CreateUserAsync("me@site.uz", "Elshod");
        var res = await user.Client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.JsonAsync();
        Assert.Equal("me@site.uz", json.GetProperty("email").GetString());
        Assert.Equal("Elshod", json.GetProperty("displayName").GetString());
        Assert.Equal("B1", json.GetProperty("level").GetString());
    }
}
