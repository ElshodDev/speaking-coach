using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Infrastructure.Jobs;

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
                        ("today", Texts.Get(lang, "bot.cmd_today")),
                        ("dars", Texts.Get(lang, "tutor.bot_cmd")),
                        ("podcast", Texts.Get(lang, "pc.cmd")),
                        ("word", Texts.Get(lang, "bot.cmd_word")),
                        ("review", Texts.Get(lang, "bot.cmd_review")),
                        ("stats", Texts.Get(lang, "bot.cmd_stats")),
                        ("settings", Texts.Get(lang, "bot.cmd_settings")),
                        ("guide", Texts.Get(lang, "bot.cmd_guide")),
                        ("lang", Texts.Get(lang, "bot.cmd_lang")),
                        ("add", Texts.Get(lang, "bot.cmd_add")),
                        ("bank", Texts.Get(lang, "bot.cmd_bank")),
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
