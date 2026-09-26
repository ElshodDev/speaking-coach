using System.Text.Json;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

public class SystemHealthTests
{
    private static readonly DateTime Now = new(2026, 9, 26, 5, 0, 0, DateTimeKind.Utc);
    private const string Url = "https://api.example.com/api/telegram/webhook";

    private static List<string> Issues(
        bool enabled = true, bool hasPublicUrl = true, string? apiError = null,
        string? actual = "SpeakingCoachUzBot", string? webhook = Url,
        int? pending = 0, DateTime? lastErrorAt = null, bool cron = true) =>
        SystemHealth.TelegramIssues(enabled, hasPublicUrl, apiError, "SpeakingCoachUzBot", actual,
            Url, webhook, pending, lastErrorAt, cron, Now);

    [Fact]
    public void Healthy_bot_has_no_issues() => Assert.Empty(Issues());

    [Fact]
    public void Disabled_bot_reports_only_off() => Assert.Equal(new List<string> { "tg_off" }, Issues(enabled: false, cron: false));

    [Fact]
    public void Unreachable_api_skips_webhook_checks() =>
        Assert.Equal(new List<string> { "tg_unreachable" }, Issues(apiError: "Telegram 401: Unauthorized", webhook: null));

    [Fact]
    public void Missing_and_foreign_webhooks_are_reported()
    {
        Assert.Contains("webhook_missing", Issues(webhook: ""));
        Assert.Contains("webhook_mismatch", Issues(webhook: "https://old.example.com/api/telegram/webhook"));
    }

    [Fact]
    public void Only_recent_webhook_errors_count()
    {
        Assert.Contains("webhook_error", Issues(lastErrorAt: Now.AddHours(-1)));
        Assert.DoesNotContain("webhook_error", Issues(lastErrorAt: Now.AddDays(-3)));
    }

    [Fact]
    public void Backlog_threshold()
    {
        Assert.DoesNotContain("webhook_backlog", Issues(pending: SystemHealth.BacklogThreshold - 1));
        Assert.Contains("webhook_backlog", Issues(pending: SystemHealth.BacklogThreshold));
    }

    [Fact]
    public void Username_compare_is_case_insensitive()
    {
        Assert.Empty(Issues(actual: "speakingcoachuzbot"));
        Assert.Contains("tg_username_mismatch", Issues(actual: "OtherBot"));
    }

    [Fact]
    public void Missing_cron_and_public_url_are_reported()
    {
        Assert.Contains("cron_missing", Issues(cron: false));
        Assert.Contains("public_url_missing", Issues(hasPublicUrl: false));
    }

    [Fact]
    public void Redact_hides_secret()
    {
        Assert.Equal("GET https://api.telegram.org/bot***/getMe failed", SystemHealth.Redact("GET https://api.telegram.org/bot123:ABC/getMe failed", "123:ABC"));
        Assert.Equal("plain", SystemHealth.Redact("plain", null));
    }

    [Fact]
    public void Missing_tables_are_case_sensitive_and_sorted()
    {
        var missing = SchemaCheck.Missing(
            ["Users", "TelegramAccounts", "AiUsages", "TelegramLinkTokens"],
            ["Users", "aiusages", "__EFMigrationsHistory"]);
        Assert.Equal(new List<string> { "AiUsages", "TelegramAccounts", "TelegramLinkTokens" }, missing);
    }

    [Fact]
    public void Schema_status_ok_rules()
    {
        Assert.True(new SchemaStatus(true, [], [], null).Ok);
        Assert.False(new SchemaStatus(true, ["20260926_AddTelegram"], [], null).Ok);
        Assert.False(new SchemaStatus(true, [], ["TelegramAccounts"], null).Ok);
        Assert.False(new SchemaStatus(false, [], [], "boom").Ok);
    }

    [Fact]
    public void Webhook_info_is_parsed()
    {
        const string json = """
            {"url":"https://x.onrender.com/api/telegram/webhook","has_custom_certificate":false,
             "pending_update_count":3,"last_error_date":1790000000,
             "last_error_message":"Wrong response from the webhook: 500 Internal Server Error","max_connections":10}
            """;
        var info = JsonSerializer.Deserialize<TgWebhookInfo>(json)!;
        Assert.Equal("https://x.onrender.com/api/telegram/webhook", info.Url);
        Assert.Equal(3, info.PendingUpdateCount);
        Assert.Equal(1790000000L, info.LastErrorDate);
        Assert.StartsWith("Wrong response", info.LastErrorMessage);

        var empty = JsonSerializer.Deserialize<TgWebhookInfo>("""{"url":"","pending_update_count":0}""")!;
        Assert.Null(empty.LastErrorDate);
    }
}
