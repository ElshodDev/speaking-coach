using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>Hisob va profil: ma'lumotni yuklab olish, hisobni o'chirish, taxallus/tanishtiruv tekshiruvlari — HTTP orqali.</summary>
public class AccountProfileHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<string> ErrorOf(HttpResponseMessage r) => (await r.JsonAsync()).GetProperty("error").GetString()!;

    [Fact]
    public async Task Protected_profile_endpoints_return_401_for_guests()
    {
        var guest = factory.CreateClient();
        foreach (var path in new[] { "/api/profile", "/api/progress", "/api/leaderboard", "/api/referral", "/api/readiness", "/api/plan/today", "/api/account/export" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PutAsync("/api/profile", JsonExtensions.Json(new { }))).StatusCode);
    }

    [Fact]
    public async Task Usage_is_open_to_guests_and_reports_the_daily_ai_budget()
    {
        var json = await (await factory.CreateClient().GetAsync("/api/usage")).JsonAsync();
        Assert.True(json.GetProperty("guest").GetBoolean());
        Assert.True(json.GetProperty("exercises").GetProperty("left").GetInt32() >= 0);
        Assert.True(json.GetProperty("words").TryGetProperty("limit", out _));
    }

    [Fact]
    public async Task Profile_update_validates_and_keeps_nicknames_unique()
    {
        var a = await factory.CreateUserAsync();
        var b = await factory.CreateUserAsync();

        var ok = await a.Client.PutAsync("/api/profile", JsonExtensions.Json(new { displayName = "Elshod", level = "b2", showOnLeaderboard = true }));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var saved = await ok.JsonAsync();
        Assert.Equal(("Elshod", "B2", true), (saved.GetProperty("displayName").GetString(), saved.GetProperty("level").GetString(), saved.GetProperty("showOnLeaderboard").GetBoolean()));

        var taken = await b.Client.PutAsync("/api/profile", JsonExtensions.Json(new { displayName = "ELSHOD", showOnLeaderboard = false }));
        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
        Assert.Equal(Msg.Err("profile.nickname_taken"), await ErrorOf(taken));

        var badName = await b.Client.PutAsync("/api/profile", JsonExtensions.Json(new { displayName = "no<script>", showOnLeaderboard = false }));
        Assert.Equal(Msg.Err("profile.bad_nickname"), await ErrorOf(badName));
        var badLevel = await b.Client.PutAsync("/api/profile", JsonExtensions.Json(new { level = "Z9", showOnLeaderboard = false }));
        Assert.Equal(Msg.Err("profile.bad_level"), await ErrorOf(badLevel));
        var needName = await b.Client.PutAsync("/api/profile", JsonExtensions.Json(new { showOnLeaderboard = true }));
        Assert.Equal(Msg.Err("profile.nickname_required"), await ErrorOf(needName));

        var profile = await (await a.Client.GetAsync("/api/profile")).JsonAsync();
        Assert.Equal("Elshod", profile.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Onboarding_saves_the_goal_and_the_profile_and_plan_reflect_it()
    {
        var u = await factory.CreateUserAsync();
        Assert.False((await (await u.Client.GetAsync("/api/profile")).JsonAsync()).GetProperty("onboarded").GetBoolean());

        var bad = await u.Client.PutAsync("/api/profile/onboarding", JsonExtensions.Json(new { skip = false, goal = "nonsense" }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Msg.Err("goal.bad_goal"), await ErrorOf(bad));

        var ok = await u.Client.PutAsync("/api/profile/onboarding", JsonExtensions.Json(new { skip = false, goal = "ielts", level = "B2", targetScore = "6.5", dailyMinutes = 20 }));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ok.JsonAsync();
        Assert.Equal(("ielts", "B2", "6.5", true), (body.GetProperty("goal").GetString(), body.GetProperty("level").GetString(), body.GetProperty("targetScore").GetString(), body.GetProperty("onboarded").GetBoolean()));

        var profile = await (await u.Client.GetAsync("/api/profile")).JsonAsync();
        Assert.True(profile.GetProperty("onboarded").GetBoolean());
        Assert.Equal("ielts", profile.GetProperty("goal").GetString());
        Assert.Equal(20, profile.GetProperty("dailyMinutes").GetInt32());

        var plan = await (await u.Client.GetAsync("/api/plan/today")).JsonAsync();
        Assert.Equal("ielts", plan.GetProperty("goal").GetString());
        Assert.Equal("6.5", plan.GetProperty("targetScore").GetString());
        Assert.Equal(JsonValueKind.Array, plan.GetProperty("items").ValueKind);
    }

    [Fact]
    public async Task Data_export_is_a_json_attachment_with_the_account_data()
    {
        var u = await factory.CreateUserAsync("export@site.uz", "Exporter");
        var res = await u.Client.GetAsync("/api/account/export");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("application/json", res.Content.Headers.ContentType!.MediaType);
        Assert.Matches(@"speaking-coach-\d{4}-\d{2}-\d{2}\.json", res.Content.Headers.ContentDisposition!.FileName!);
        var json = await res.JsonAsync();
        Assert.Equal("export@site.uz", json.GetProperty("account").GetProperty("Email").GetString());
        Assert.False(json.GetProperty("account").GetProperty("hasPassword").GetBoolean());
        foreach (var key in new[] { "activities", "reviewCards", "reviewLog", "groupsTaught", "feedback", "sessions", "aiUsage" })
            Assert.Equal(JsonValueKind.Array, json.GetProperty(key).ValueKind);
        Assert.False(json.ToString().Contains("TokenHash", StringComparison.OrdinalIgnoreCase));          // sessiya sirlari eksportga kirmaydi
    }

    [Fact]
    public async Task Deleting_the_account_needs_the_email_and_then_removes_the_data_and_the_session()
    {
        var u = await factory.CreateUserAsync("bye@site.uz");
        var wrong = await u.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/account") { Content = JsonExtensions.Json(new { confirmEmail = "other@site.uz" }) });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal(Msg.Err("account.confirm_mismatch"), await ErrorOf(wrong));

        var ok = await u.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/account") { Content = JsonExtensions.Json(new { confirmEmail = "BYE@site.uz" }) });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True((await ok.JsonAsync()).GetProperty("deleted").GetBoolean());

        Assert.Equal(HttpStatusCode.Unauthorized, (await u.Client.GetAsync("/api/profile")).StatusCode);   // sessiya ham o'chdi
        Assert.Equal(0, await factory.WithDbAsync(db => db.Users.CountAsync(x => x.Email == "bye@site.uz")));
    }
}
