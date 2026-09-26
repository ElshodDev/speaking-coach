using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Services;

public record TelegramStatus(
    bool Enabled,
    string BotUsername,
    string? ActualUsername,
    string? ExpectedWebhookUrl,
    string? WebhookUrl,
    int? PendingUpdates,
    DateTime? LastErrorAtUtc,
    string? LastError,
    bool CronConfigured,
    int LinkedAccounts,
    int RemindersOn,
    string? ApiError,
    IReadOnlyList<string> Issues);

public record FeatureStatus(bool EmailVerification, bool GoogleSignIn, bool Gemini);

public record SystemStatus(SchemaStatus Database, TelegramStatus Telegram, FeatureStatus Features);

/// <summary>
/// Admin panel uchun "Tizim holati": baza sxemasi, Telegram bot va asosiy
/// sozlamalar. Hech qanday sir (token, parol, kalit) qaytarilmaydi — faqat
/// "sozlangan/sozlanmagan" va Telegram'ning o'zi bergan ochiq ma'lumotlar.
/// </summary>
public static class SystemHealth
{
    /// <summary>Webhook xatosi shu muddatdan yangi bo'lsa — hozirgi muammo deb hisoblanadi.</summary>
    public static readonly TimeSpan RecentError = TimeSpan.FromHours(24);

    /// <summary>Telegram'da shuncha xabar yetkazilmay turgan bo'lsa — server javob bermayapti.</summary>
    public const int BacklogThreshold = 20;

    /// <summary>
    /// Muammolar ro'yxati (kalitlar; matnni frontend o'z tilida chiqaradi).
    /// Tartib — eng jiddiysidan boshlab.
    /// </summary>
    public static List<string> TelegramIssues(
        bool enabled, bool hasPublicUrl, string? apiError,
        string configuredUsername, string? actualUsername,
        string? expectedWebhookUrl, string? webhookUrl,
        int? pendingUpdates, DateTime? lastErrorAtUtc, bool cronConfigured, DateTime nowUtc)
    {
        var issues = new List<string>();
        if (!enabled) return ["tg_off"];
        if (!hasPublicUrl) issues.Add("public_url_missing");
        if (apiError is not null)
        {
            issues.Add("tg_unreachable");
        }
        else
        {
            if (actualUsername is not null && !string.Equals(actualUsername, configuredUsername, StringComparison.OrdinalIgnoreCase))
                issues.Add("tg_username_mismatch");
            if (string.IsNullOrEmpty(webhookUrl)) issues.Add("webhook_missing");
            else if (expectedWebhookUrl is not null && webhookUrl != expectedWebhookUrl) issues.Add("webhook_mismatch");
            if (lastErrorAtUtc is DateTime at && nowUtc - at < RecentError) issues.Add("webhook_error");
            if (pendingUpdates >= BacklogThreshold) issues.Add("webhook_backlog");
        }
        if (!cronConfigured) issues.Add("cron_missing");
        return issues;
    }

    /// <summary>Xato matnida token tasodifan bo'lsa — yashiramiz (masalan, URL bilan birga kelgan xato).</summary>
    public static string Redact(string text, string? secret) =>
        string.IsNullOrEmpty(secret) ? text : text.Replace(secret, "***", StringComparison.Ordinal);

    public static async Task<SystemStatus> GetAsync(
        AppDbContext db, ITelegramApi api, TelegramOptions tg, IEmailSender email, IGoogleSignIn google,
        IConfiguration config, CancellationToken ct = default)
    {
        var schema = await SchemaCheck.InspectAsync(db, ct);

        string? actual = null, webhookUrl = null, lastError = null, apiError = null;
        int? pending = null;
        DateTime? lastErrorAt = null;
        var expected = tg.PublicUrl is null ? null : $"{tg.PublicUrl}/api/telegram/webhook";

        if (tg.Enabled)
        {
            try
            {
                actual = (await api.GetMeAsync(ct)).Username;
                var info = await api.GetWebhookInfoAsync(ct);
                webhookUrl = info.Url;
                pending = info.PendingUpdateCount;
                lastError = info.LastErrorMessage;
                lastErrorAt = info.LastErrorDate is long d ? DateTimeOffset.FromUnixTimeSeconds(d).UtcDateTime : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                apiError = Redact(ex.Message, tg.Token);
            }
        }

        // Jadval yo'q bo'lsa (migratsiya qo'llanmagan) — sanoq o'rniga 0, sahifa yiqilmaydi.
        int linked = 0, reminders = 0;
        if (schema.Ok)
        {
            linked = await db.TelegramAccounts.CountAsync(_ => true, ct);
            reminders = await db.TelegramAccounts.CountAsync(a => a.ReminderHour != null, ct);
        }

        var issues = TelegramIssues(tg.Enabled, tg.PublicUrl is not null, apiError, tg.BotUsername, actual,
            expected, webhookUrl, pending, lastErrorAt, tg.CronSecret is not null, DateTime.UtcNow);

        return new SystemStatus(
            schema,
            new TelegramStatus(tg.Enabled, tg.BotUsername, actual, expected, webhookUrl, pending, lastErrorAt,
                lastError, tg.CronSecret is not null, linked, reminders, apiError, issues),
            new FeatureStatus(email.IsConfigured, google.ClientId is not null, !string.IsNullOrWhiteSpace(config["Gemini:ApiKey"])));
    }
}
