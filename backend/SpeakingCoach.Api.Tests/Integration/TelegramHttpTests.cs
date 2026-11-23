using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>Telegram: webhook/cron (maxfiy sarlavhalar) va saytdan ulash endpointlari — bot yoqiq va o'chiq holatda.</summary>
public class TelegramEnabledHttpTests(TelegramApiFactory factory) : IClassFixture<TelegramApiFactory>
{
    private string Secret => factory.Services.GetRequiredService<TelegramOptions>().WebhookSecret!;

    private static HttpRequestMessage Webhook(string? secret, string body = "{}")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/telegram/webhook") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (secret is not null) req.Headers.Add("X-Telegram-Bot-Api-Secret-Token", secret);
        return req;
    }

    [Fact]
    public async Task Webhook_rejects_a_wrong_or_missing_secret_and_always_answers_200_otherwise()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Webhook(null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Webhook("wrong-secret"))).StatusCode);

        // To'g'ri sarlavha: buzuq JSON ham, bo'sh yangilanish ham 200 (aks holda Telegram qayta-qayta yuboraveradi).
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Webhook(Secret, "not json"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Webhook(Secret, "{}"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Webhook(Secret, "null"))).StatusCode);
    }

    [Fact]
    public async Task Cron_needs_the_cron_secret()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/telegram/cron", null)).StatusCode);
        var wrong = new HttpRequestMessage(HttpMethod.Post, "/api/telegram/cron");
        wrong.Headers.Add("X-Cron-Secret", "nope");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(wrong)).StatusCode);
    }

    [Fact]
    public async Task Site_endpoints_need_a_login()
    {
        var guest = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/api/telegram/status")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/telegram/link", JsonExtensions.Json(new { tzOffsetMinutes = 0 }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PutAsync("/api/telegram/settings", JsonExtensions.Json(new { reminderHour = 9 }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.DeleteAsync("/api/telegram/link")).StatusCode);
    }

    [Fact]
    public async Task Linking_flow_status_link_reminder_hour_and_unlink()
    {
        var u = await factory.CreateUserAsync();
        var status = await (await u.Client.GetAsync("/api/telegram/status")).JsonAsync();
        Assert.True(status.GetProperty("enabled").GetBoolean());
        Assert.Equal("TestBot", status.GetProperty("botUsername").GetString());
        Assert.False(status.GetProperty("linked").GetBoolean());

        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.PutAsync("/api/telegram/settings", JsonExtensions.Json(new { reminderHour = 9 }))).StatusCode);   // ulanmagan

        var link = await (await u.Client.PostAsync("/api/telegram/link", JsonExtensions.Json(new { tzOffsetMinutes = 300, lang = "ru" }))).JsonAsync();
        Assert.StartsWith("https://t.me/TestBot?start=", link.GetProperty("url").GetString());

        // Bot ulanishni tasdiqlagandek: hisob bog'landi.
        await factory.WithDbAsync(async db => { db.TelegramAccounts.Add(new TelegramAccount { ChatId = 4242, UserId = u.Id, Username = "elshod" }); await db.SaveChangesAsync(); });
        var linked = await (await u.Client.GetAsync("/api/telegram/status")).JsonAsync();
        Assert.True(linked.GetProperty("linked").GetBoolean());
        Assert.Equal("elshod", linked.GetProperty("username").GetString());

        var hour = await (await u.Client.PutAsync("/api/telegram/settings", JsonExtensions.Json(new { reminderHour = 99 }))).JsonAsync();
        Assert.Equal(23, hour.GetProperty("reminderHour").GetInt32());                            // 0–23 ga qisqartiriladi
        Assert.Equal(HttpStatusCode.OK, (await u.Client.DeleteAsync("/api/telegram/link")).StatusCode);
        Assert.False((await (await u.Client.GetAsync("/api/telegram/status")).JsonAsync()).GetProperty("linked").GetBoolean());
    }

    [Fact]
    public async Task Demo_accounts_cannot_link_a_bot()
    {
        var demo = await factory.CreateUserAsync($"demo-{Guid.NewGuid():N}@{DemoAccount.Domain}");
        var res = await demo.Client.PostAsync("/api/telegram/link", JsonExtensions.Json(new { tzOffsetMinutes = 0 }));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(Msg.Err("demo.not_allowed"), (await res.JsonAsync()).GetProperty("error").GetString());
    }
}

public class TelegramDisabledHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Without_a_bot_token_the_webhook_cron_and_link_do_not_exist()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/telegram/webhook", JsonExtensions.Json(new { }))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/telegram/cron", null)).StatusCode);

        var u = await factory.CreateUserAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await u.Client.PostAsync("/api/telegram/link", JsonExtensions.Json(new { tzOffsetMinutes = 0 }))).StatusCode);
        var status = await (await u.Client.GetAsync("/api/telegram/status")).JsonAsync();
        Assert.False(status.GetProperty("enabled").GetBoolean());                                 // sayt shunga qarab kartani yashiradi
    }
}
