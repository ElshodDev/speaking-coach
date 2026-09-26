using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Telegram;

public record ReminderRun(int Checked, int Sent, int SkippedGoalDone, int Disabled);

/// <summary>
/// Kunlik eslatmalar. Render'ning bepul serveri uxlab qoladi va o'zi
/// taymer bilan ishlay olmaydi — shuning uchun GitHub Actions har soatda
/// /api/telegram/cron'ni chaqiradi (serverni uyg'otadi), bu yerda esa
/// vaqti kelgan eslatmalar yuboriladi. Qoida: kuniga ko'pi bilan BITTA
/// xabar; bugungi maqsad bajarilgan bo'lsa — umuman yozmaymiz.
/// </summary>
public class TelegramReminders
{
    private readonly AppDbContext _db;
    private readonly ITelegramApi _api;
    private readonly ReviewService _reviews;
    private readonly ILogger<TelegramReminders> _logger;

    public TelegramReminders(AppDbContext db, ITelegramApi api, ReviewService reviews, ILogger<TelegramReminders> logger)
    {
        _db = db;
        _api = api;
        _reviews = reviews;
        _logger = logger;
    }

    public async Task<ReminderRun> RunAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var accounts = await _db.TelegramAccounts.Where(a => a.ReminderHour != null).ToListAsync(ct);
        int sent = 0, goalDone = 0, disabled = 0, checkedCount = 0;

        foreach (var a in accounts.Where(a => BotLogic.ShouldRemind(a.ReminderHour, a.LastReminderDate, nowUtc, a.TzOffsetMinutes)))
        {
            checkedCount++;
            a.LastReminderDate = BotLogic.LocalToday(nowUtc, a.TzOffsetMinutes);
            try
            {
                var s = await _reviews.GetStatsAsync(a.UserId, a.TzOffsetMinutes);
                if (s.ReviewedToday >= s.DailyGoal)
                {
                    goalDone++;
                    continue; // bugun allaqachon shug'ullangan — bezovta qilmaymiz
                }

                var head = s.StreakDays > 0 ? Texts.Get(a.Lang, "bot.reminder_streak", s.StreakDays) : Texts.Get(a.Lang, "bot.reminder_start");
                var body = s.Due > 0 ? Texts.Get(a.Lang, "bot.reminder_due", s.Due) : Texts.Get(a.Lang, "bot.reminder_nodue");
                await _api.SendMessageAsync(a.ChatId, $"{head}\n{body}", new[]
                {
                    new[] { new TgButton(Texts.Get(a.Lang, "bot.start_button"), BotLogic.Encode(new BotCallback.StartReview())) },
                }, ct: ct);
                sent++;
            }
            catch (TelegramApiException ex) when (ex.ChatGone)
            {
                // Foydalanuvchi botni bloklagan — eslatmalarni o'chiramiz, boshqa urinmaymiz.
                a.ReminderHour = null;
                disabled++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Eslatma yuborilmadi: chat {Chat}", a.ChatId);
            }
        }

        await _db.SaveChangesAsync(ct);
        return new ReminderRun(checkedCount, sent, goalDone, disabled);
    }
}

/// <summary>
/// Server ishga tushganda Telegram'ga webhook manzilini va buyruqlar
/// ro'yxatini (uch tilda) aytadi. Takror chaqirish zararsiz. Ilova ishga
/// tushishini to'xtatmaydi — xato bo'lsa, faqat logga yoziladi.
/// </summary>
public class TelegramStartup : BackgroundService
{
    private readonly ITelegramApi _api;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramStartup> _logger;

    public TelegramStartup(ITelegramApi api, TelegramOptions options, ILogger<TelegramStartup> logger)
    {
        _api = api;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || _options.PublicUrl is null)
        {
            _logger.LogInformation("Telegram bot o'chiq (BotToken yoki PublicUrl yo'q)");
            return;
        }

        for (var attempt = 1; attempt <= 3 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                await _api.SetWebhookAsync($"{_options.PublicUrl}/api/telegram/webhook", _options.WebhookSecret!, stoppingToken);
                foreach (var lang in Texts.Langs)
                {
                    var commands = new[]
                    {
                        ("review", Texts.Get(lang, "bot.cmd_review")),
                        ("stats", Texts.Get(lang, "bot.cmd_stats")),
                        ("settings", Texts.Get(lang, "bot.cmd_settings")),
                        ("help", Texts.Get(lang, "bot.cmd_help")),
                    };
                    // O'zbekcha — standart (language_code yo'q), rus va ingliz — o'z tilida.
                    await _api.SetCommandsAsync(commands, lang == "uz" ? null : lang, stoppingToken);
                }
                _logger.LogInformation("Telegram webhook o'rnatildi: {Url}/api/telegram/webhook", _options.PublicUrl);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Telegram webhook'ni o'rnatib bo'lmadi (urinish {Attempt})", attempt);
                await Task.Delay(TimeSpan.FromSeconds(10 * attempt), stoppingToken);
            }
        }
    }
}
