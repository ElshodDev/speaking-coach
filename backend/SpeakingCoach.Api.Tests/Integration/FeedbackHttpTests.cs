using System.Net;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>Fikr va bir bosishli baholar: guest ham yuboradi, admin ro'yxatlari faqat adminga.</summary>
public class FeedbackHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Feedback_is_validated_and_guests_can_send_it()
    {
        var guest = factory.CreateClient();
        var bad = await guest.PostAsync("/api/feedback", JsonExtensions.Json(new { kind = "weird", message = "something" }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var err = await bad.JsonAsync();
        Assert.Equal(Msg.Err("feedback.bad_kind"), err.GetProperty("error").GetString());
        Assert.True(err.TryGetProperty("code", out _));                                       // ErrorWithCode: mashinaga o'qiladigan kod ham bor

        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsync("/api/feedback", JsonExtensions.Json(new { kind = "bug", message = "x" }))).StatusCode);

        var ok = await guest.PostAsync("/api/feedback", JsonExtensions.Json(new { kind = " IDEA ", message = "Add a dark mode please", page = "/#/profile" }));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var json = await ok.JsonAsync();
        Assert.True(json.GetProperty("ok").GetBoolean());
        Assert.True(json.GetProperty("id").GetGuid() != Guid.Empty);
    }

    [Fact]
    public async Task Admin_feedback_list_is_for_admins_only()
    {
        var user = await factory.CreateUserAsync();
        var admin = await factory.CreateUserAsync(ApiFactory.AdminEmail);
        await user.Client.PostAsync("/api/feedback", JsonExtensions.Json(new { kind = "bug", message = "Button is broken" }));

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/admin/feedback")).StatusCode);
        var forbidden = await user.Client.GetAsync("/api/admin/feedback");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(Msg.Err("admin.only"), (await forbidden.JsonAsync()).GetProperty("error").GetString());

        var list = await (await admin.Client.GetAsync("/api/admin/feedback")).JsonAsync();
        var item = list.EnumerateArray().First(x => x.GetProperty("message").GetString() == "Button is broken");
        Assert.Equal("bug", item.GetProperty("kind").GetString());
        Assert.Equal(user.Email, item.GetProperty("userEmail").GetString());                      // admin yozuvchini ko'radi
    }

    [Fact]
    public async Task Ratings_use_an_allow_list_and_one_time_kinds_are_not_repeated()
    {
        var u = await factory.CreateUserAsync();
        var bad = await u.Client.PostAsync("/api/rating", JsonExtensions.Json(new { key = "free text not allowed" }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Msg.Err("feedback.bad_kind"), (await bad.JsonAsync()).GetProperty("error").GetString());

        var first = await (await u.Client.PostAsync("/api/rating", JsonExtensions.Json(new { key = "first_practice:up", page = "/#/practice" }))).JsonAsync();
        Assert.True(first.GetProperty("ok").GetBoolean());
        Assert.False(first.TryGetProperty("duplicate", out _));
        var again = await (await u.Client.PostAsync("/api/rating", JsonExtensions.Json(new { key = "first_practice:down" }))).JsonAsync();
        Assert.True(again.GetProperty("duplicate").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().PostAsync("/api/rating", JsonExtensions.Json(new { key = "source:friend" }))).StatusCode);   // mehmon ham baholaydi
    }

    [Fact]
    public async Task Admin_rating_summary_groups_by_key()
    {
        var admin = await factory.CreateUserAsync(ApiFactory.AdminEmail);
        var u = await factory.CreateUserAsync();
        await u.Client.PostAsync("/api/rating", JsonExtensions.Json(new { key = "lesson:up" }));
        await factory.CreateClient().PostAsync("/api/rating", JsonExtensions.Json(new { key = "lesson:up" }));

        Assert.Equal(HttpStatusCode.Forbidden, (await u.Client.GetAsync("/api/admin/ratings")).StatusCode);
        var json = await (await admin.Client.GetAsync("/api/admin/ratings?days=9999")).JsonAsync();
        Assert.Equal(365, json.GetProperty("days").GetInt32());                                   // chegaraga qisqartirildi
        var lesson = json.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("key").GetString() == "lesson:up");
        Assert.True(lesson.GetProperty("count").GetInt32() >= 2);
    }
}
