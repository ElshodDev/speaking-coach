using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// Telegram'dan kelgan har bir yangilanish (xabar yoki tugma bosilishi)
/// shu yerda qayta ishlanadi. Bot — saytning yana bir "ko'rinishi": kartalar,
/// lug'at, seriya va AI limiti saytdagi bilan bir xil (bitta baza, bitta
/// ReviewService). Xatolar foydalanuvchiga tushunarli xabar bilan qaytadi va
/// hech qachon webhook'ni yiqitmaydi.
/// </summary>
public class TelegramBot
{
    // Telegram javobni kutib qolsa, xuddi shu yangilanishni qayta yuborishi
    // mumkin — oxirgi ID'larni eslab, ikki marta ishlamaymiz.
    private static readonly ConcurrentDictionary<long, byte> Seen = new();
    private static readonly ConcurrentQueue<long> SeenOrder = new();

    // "Lug'atga qo'shish" tugmasi uchun oxirgi izohlar: qayta Gemini'ga
    // murojaat qilmaslik (va limitni sarflamaslik) uchun. Server qayta ishga
    // tushsa yo'qoladi — unda izoh qayta olinadi.
    private static readonly ConcurrentDictionary<(long Chat, string Word), WordExplanation> RecentWords = new();

    private readonly AppDbContext _db;
    private readonly ITelegramApi _api;
    private readonly TelegramOptions _options;
    private readonly ReviewService _reviews;
    private readonly IWordService _words;
    private readonly AiQuotaService _quotas;
    private readonly ILogger<TelegramBot> _logger;

    public TelegramBot(AppDbContext db, ITelegramApi api, TelegramOptions options, ReviewService reviews,
        IWordService words, AiQuotaService quotas, ILogger<TelegramBot> logger)
    {
        _db = db;
        _api = api;
        _options = options;
        _reviews = reviews;
        _words = words;
        _quotas = quotas;
        _logger = logger;
    }

    private sealed record Ctx(long ChatId, TgUser From, TelegramAccount? Account, User? User, string Lang)
    {
        public string T(string key, params object?[] args) => Texts.Get(Lang, key, args);
    }

    public static bool MarkSeen(long updateId)
    {
        if (!Seen.TryAdd(updateId, 0)) return false;
        SeenOrder.Enqueue(updateId);
        while (SeenOrder.Count > 1000 && SeenOrder.TryDequeue(out var old)) Seen.TryRemove(old, out _);
        return true;
    }

    public async Task HandleAsync(TgUpdate update, CancellationToken ct = default)
    {
        if (!MarkSeen(update.UpdateId)) return;

        if (update.CallbackQuery is { } cb)
        {
            await HandleCallbackAsync(cb, ct);
        }
        else if (update.Message is { From: not null } msg && msg.Chat.Type == "private")
        {
            await HandleMessageAsync(msg, ct);
        }
    }

    private async Task<Ctx> ContextAsync(long chatId, TgUser from, CancellationToken ct)
    {
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(a => a.ChatId == chatId, ct);
        var user = account is null ? null : await _db.Users.FirstOrDefaultAsync(u => u.Id == account.UserId, ct);
        return new Ctx(chatId, from, account, user, account?.Lang ?? BotLogic.LangFromTelegram(from.LanguageCode));
    }

    // ---------------- Xabarlar ----------------

    private async Task HandleMessageAsync(TgMessage msg, CancellationToken ct)
    {
        var c = await ContextAsync(msg.Chat.Id, msg.From!, ct);
        var text = msg.Text?.Trim() ?? "";
        try
        {
            var start = BotLogic.StartPayload(text);
            if (start is not null)
            {
                if (start.Length > 0) await LinkAsync(c, start, ct);
                else await WelcomeAsync(c, ct);
                return;
            }

            switch (BotLogic.ParseMenu(text))
            {
                case MenuAction.Review:
                    if (await RequireLinkAsync(c, ct)) await SendNextCardAsync(c, ct);
                    return;
                case MenuAction.Stats:
                    if (await RequireLinkAsync(c, ct)) await SendStatsAsync(c, ct);
                    return;
                case MenuAction.Settings:
                    if (await RequireLinkAsync(c, ct)) await _api.SendMessageAsync(c.ChatId, SettingsText(c), SettingsButtons(c), ct: ct);
                    return;
                case MenuAction.Help:
                    await _api.SendMessageAsync(c.ChatId, c.T("bot.help"), replyKeyboard: c.User is null ? null : BotLogic.MainMenu(c.Lang), ct: ct);
                    return;
            }

            if (BotLogic.IsLookupWord(text))
            {
                await ExplainAsync(c, text.Trim(), ct);
                return;
            }

            await _api.SendMessageAsync(c.ChatId, c.T("bot.not_word"), ct: ct);
        }
        catch (TelegramApiException ex)
        {
            _logger.LogWarning(ex, "Telegram javobi: chat {Chat}", c.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bot xabarini qayta ishlashda xato: chat {Chat}", c.ChatId);
            await TrySendAsync(c.ChatId, c.T("bot.error"), ct);
        }
    }

    private async Task WelcomeAsync(Ctx c, CancellationToken ct)
    {
        if (c.User is not null)
        {
            await _api.SendMessageAsync(c.ChatId, c.T("bot.help"), replyKeyboard: BotLogic.MainMenu(c.Lang), ct: ct);
            return;
        }
        await _api.SendMessageAsync(c.ChatId, c.T("bot.welcome"), LinkButton(c), ct: ct);
    }

    private IReadOnlyList<IReadOnlyList<TgButton>> LinkButton(Ctx c) =>
        new[] { new[] { new TgButton(c.T("bot.link_button"), Url: $"{_options.FrontendUrl}/#/profile") } };

    private async Task<bool> RequireLinkAsync(Ctx c, CancellationToken ct)
    {
        if (c.User is not null) return true;
        await _api.SendMessageAsync(c.ChatId, c.T("bot.need_link"), LinkButton(c), ct: ct);
        return false;
    }

    /// <summary>/start TOKEN — saytdagi "Telegramni ulash" havolasi.</summary>
    private async Task LinkAsync(Ctx c, string token, CancellationToken ct)
    {
        var hash = BotLogic.HashToken(token);
        var now = DateTime.UtcNow;
        var link = await _db.TelegramLinkTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.ExpiresAtUtc > now, ct);
        var user = link is null ? null : await _db.Users.FirstOrDefaultAsync(u => u.Id == link.UserId, ct);
        if (link is null || user is null)
        {
            await _api.SendMessageAsync(c.ChatId, c.T("bot.link_invalid"), LinkButton(c), ct: ct);
            return;
        }

        // Bitta hisob — bitta chat: eski bog'lanishlar (shu chat yoki shu hisob) almashtiriladi.
        _db.TelegramAccounts.RemoveRange(await _db.TelegramAccounts
            .Where(a => a.ChatId == c.ChatId || a.UserId == user.Id).ToListAsync(ct));
        var account = new TelegramAccount
        {
            ChatId = c.ChatId,
            UserId = user.Id,
            Username = c.From.Username,
            Lang = link.Lang,
            TzOffsetMinutes = link.TzOffsetMinutes,
            ReminderHour = 20,
            LinkedAtUtc = now,
        };
        _db.TelegramAccounts.Add(account);
        _db.TelegramLinkTokens.Remove(link);
        await _db.SaveChangesAsync(ct);

        var lang = account.Lang;
        await _api.SendMessageAsync(
            c.ChatId,
            Texts.Get(lang, "bot.linked", BotLogic.Html(BotLogic.MaskEmail(user.Email)), "20:00"),
            replyKeyboard: BotLogic.MainMenu(lang),
            ct: ct);
    }

    // ---------------- Takrorlash ----------------

    private string PromptFor(Ctx c, ReviewCardKind kind) => c.T(kind switch
    {
        ReviewCardKind.Correction => "bot.prompt_correction",
        ReviewCardKind.Question => "bot.prompt_question",
        _ => "bot.prompt_word",
    });

    private static string Front(ReviewCard card) =>
        card.Kind == ReviewCardKind.Correction ? $"<s>{BotLogic.Html(card.Front)}</s>" : $"<b>{BotLogic.Html(card.Front)}</b>";

    private async Task SendNextCardAsync(Ctx c, CancellationToken ct)
    {
        var due = await _reviews.GetDueAsync(c.User!.Id, 50);
        if (due.Count == 0)
        {
            var stats = await _reviews.GetStatsAsync(c.User.Id, c.Account!.TzOffsetMinutes);
            var text = stats.Total == 0 ? c.T("bot.no_cards") : c.T("bot.all_done", stats.StreakDays, stats.ReviewedToday);
            await _api.SendMessageAsync(c.ChatId, text, replyKeyboard: BotLogic.MainMenu(c.Lang), ct: ct);
            return;
        }

        var card = due[0];
        var html = $"{PromptFor(c, card.Kind)}\n\n{Front(card)}\n\n<i>{c.T("bot.left", due.Count)}</i>";
        await _api.SendMessageAsync(c.ChatId, html, new[]
        {
            new[] { new TgButton(c.T("bot.show_answer"), BotLogic.Encode(new BotCallback.ShowAnswer(card.Id))) },
        }, ct: ct);
    }

    private static string AnswerBlock(ReviewCard card)
    {
        var note = string.IsNullOrWhiteSpace(card.Note) ? "" : $"\n<i>{BotLogic.Html(card.Note)}</i>";
        return $"➡️ <b>{BotLogic.Html(card.Back)}</b>{note}";
    }

    // ---------------- So'z izohi ----------------

    private async Task ExplainAsync(Ctx c, string word, CancellationToken ct)
    {
        var guestKey = $"tg:{c.ChatId}";
        var denied = await _quotas.CheckAsync(c.User, guestKey, AiKind.Word);
        if (denied is not null)
        {
            await _api.SendMessageAsync(c.ChatId, c.T(denied.Value.Key, denied.Value.Args), ct: ct);
            return;
        }

        var e = await _words.ExplainAsync(word, "", c.User?.Level ?? LearnerLevel.Default, c.Lang, ct);
        await _quotas.RecordAsync(c.User, guestKey, AiKind.Word);
        RecentWords[(c.ChatId, e.Word.ToLowerInvariant())] = e;
        if (RecentWords.Count > 5000) RecentWords.Clear();

        var html =
            $"<b>{BotLogic.Html(e.Word)}</b> <i>({BotLogic.Html(e.PartOfSpeech)})</i>\n" +
            $"🔤 <b>{BotLogic.Html(e.Translation)}</b>\n\n" +
            $"📖 {BotLogic.Html(e.DefinitionEn)}\n" +
            $"💬 <i>{BotLogic.Html(e.Example)}</i>";

        if (c.User is null)
        {
            await _api.SendMessageAsync(c.ChatId, $"{html}\n\n{c.T("bot.guest_hint")}", LinkButton(c), ct: ct);
            return;
        }
        await _api.SendMessageAsync(c.ChatId, html, new[]
        {
            new[] { new TgButton(c.T("bot.add_button"), BotLogic.Encode(new BotCallback.AddWord(e.Word.ToLowerInvariant()))) },
        }, ct: ct);
    }

    // ---------------- Natijalar va sozlamalar ----------------

    private async Task SendStatsAsync(Ctx c, CancellationToken ct)
    {
        var s = await _reviews.GetStatsAsync(c.User!.Id, c.Account!.TzOffsetMinutes);
        var html = c.T("bot.stats", s.StreakDays, Math.Min(s.ReviewedToday, s.DailyGoal), s.DailyGoal, s.Due, s.Total);
        var buttons = new List<TgButton[]>();
        if (s.Due > 0) buttons.Add(new[] { new TgButton(c.T("bot.start_button"), BotLogic.Encode(new BotCallback.StartReview())) });
        buttons.Add(new[] { new TgButton(c.T("bot.open_site"), Url: $"{_options.FrontendUrl}/#/progress") });
        await _api.SendMessageAsync(c.ChatId, html, buttons, ct: ct);
    }

    private static string LangName(string lang) => lang switch { "ru" => "Русский", "en" => "English", _ => "Oʻzbekcha" };

    private static string SettingsText(Ctx c) => c.T(
        "bot.settings",
        c.Account!.ReminderHour is int h ? $"{h:00}:00" : c.T("bot.reminder_off"),
        LangName(c.Account.Lang));

    private static IReadOnlyList<IReadOnlyList<TgButton>> SettingsButtons(Ctx c)
    {
        var current = c.Account!.ReminderHour;
        string Mark(bool on, string label) => on ? $"✓ {label}" : label;
        return new[]
        {
            BotLogic.ReminderHours
                .Select(h => new TgButton(Mark(current == h, $"{h:00}:00"), BotLogic.Encode(new BotCallback.SetReminder(h))))
                .ToArray(),
            new[] { new TgButton(Mark(current is null, c.T("bot.reminder_off_button")), BotLogic.Encode(new BotCallback.SetReminder(null))) },
            Texts.Langs.Select(l => new TgButton(Mark(c.Account.Lang == l, l.ToUpperInvariant()), BotLogic.Encode(new BotCallback.SetLang(l)))).ToArray(),
            new[] { new TgButton(c.T("bot.unlink_button"), BotLogic.Encode(new BotCallback.Unlink())) },
        };
    }

    // ---------------- Tugmalar ----------------

    private async Task HandleCallbackAsync(TgCallbackQuery cb, CancellationToken ct)
    {
        var chatId = cb.Message?.Chat.Id ?? cb.From.Id;
        var messageId = cb.Message?.MessageId;
        var c = await ContextAsync(chatId, cb.From, ct);
        string? toast = null;
        try
        {
            var action = BotLogic.Decode(cb.Data);
            if (action is null) return;
            if (c.User is null)
            {
                await _api.SendMessageAsync(chatId, c.T("bot.need_link"), LinkButton(c), ct: ct);
                return;
            }

            switch (action)
            {
                case BotCallback.StartReview:
                    await SendNextCardAsync(c, ct);
                    break;

                case BotCallback.ShowAnswer show:
                {
                    var card = await _db.ReviewCards.FirstOrDefaultAsync(x => x.Id == show.CardId && x.UserId == c.User.Id, ct);
                    if (card is null || messageId is null)
                    {
                        toast = c.T("bot.card_missing");
                        break;
                    }
                    var html = $"{PromptFor(c, card.Kind)}\n\n{Front(card)}\n\n{AnswerBlock(card)}\n\n{c.T("bot.how_well")}";
                    TgButton G(ReviewGrade g) => new(c.T($"bot.grade_{(int)g}"), BotLogic.Encode(new BotCallback.Grade(card.Id, g)));
                    await _api.EditMessageAsync(chatId, messageId.Value, html, new[]
                    {
                        new[] { G(ReviewGrade.Again), G(ReviewGrade.Hard) },
                        new[] { G(ReviewGrade.Good), G(ReviewGrade.Easy) },
                    }, ct);
                    break;
                }

                case BotCallback.Grade grade:
                {
                    var card = await _reviews.GradeAsync(c.User.Id, grade.CardId, grade.Value);
                    if (card is null)
                    {
                        toast = c.T("bot.card_missing");
                        break;
                    }
                    if (messageId is not null)
                    {
                        // Baholangan kartani ixcham ko'rinishga keltiramiz — chat tarixida qisqa qolsin.
                        var summary = $"{Front(card)} → {BotLogic.Html(card.Back)}\n<i>{c.T($"bot.grade_{(int)grade.Value}")}</i>";
                        await _api.EditMessageAsync(chatId, messageId.Value, summary, ct: ct);
                    }
                    await SendNextCardAsync(c, ct);
                    break;
                }

                case BotCallback.AddWord add:
                {
                    var e = RecentWords.TryGetValue((chatId, add.Word), out var cached)
                        ? cached
                        : await _words.ExplainAsync(add.Word, "", c.User.Level, c.Lang, ct);
                    var added = await _reviews.AddCardsAsync(c.User.Id, null, new[]
                    {
                        new NewCard(ReviewCardKind.Word, e.Word, e.Translation, Vocabulary.ComposeNote(e.PartOfSpeech, e.DefinitionEn, e.Example)),
                    });
                    if (added > 0) await _db.SaveChangesAsync(ct);
                    toast = c.T(added > 0 ? "bot.added" : "bot.exists");
                    if (messageId is not null && cb.Message?.Text is { } original)
                    {
                        // Tugma o'rniga holat — ikkinchi marta bosib bo'lmasin.
                        await _api.EditMessageAsync(chatId, messageId.Value,
                            $"{BotLogic.Html(original)}\n\n{toast}", ct: ct);
                    }
                    break;
                }

                case BotCallback.SetReminder r:
                    c.Account!.ReminderHour = r.Hour;
                    await _db.SaveChangesAsync(ct);
                    toast = c.T("bot.saved");
                    if (messageId is not null) await _api.EditMessageAsync(chatId, messageId.Value, SettingsText(c), SettingsButtons(c), ct);
                    break;

                case BotCallback.SetLang l:
                {
                    c.Account!.Lang = l.Lang;
                    await _db.SaveChangesAsync(ct);
                    var nc = c with { Lang = l.Lang };
                    toast = nc.T("bot.saved");
                    if (messageId is not null) await _api.EditMessageAsync(chatId, messageId.Value, SettingsText(nc), SettingsButtons(nc), ct);
                    // Pastdagi menyu ham yangi tilda.
                    await _api.SendMessageAsync(chatId, nc.T("bot.saved"), replyKeyboard: BotLogic.MainMenu(nc.Lang), ct: ct);
                    break;
                }

                case BotCallback.Unlink:
                    _db.TelegramAccounts.Remove(c.Account!);
                    await _db.SaveChangesAsync(ct);
                    if (messageId is not null) await _api.EditMessageAsync(chatId, messageId.Value, c.T("bot.unlinked"), ct: ct);
                    break;
            }
        }
        catch (TelegramApiException ex)
        {
            _logger.LogWarning(ex, "Telegram javobi (callback): chat {Chat}", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bot tugmasini qayta ishlashda xato: chat {Chat}", chatId);
            toast = c.T("bot.error");
        }
        finally
        {
            // Telegram "soat" belgisini o'chirishi uchun har doim javob beramiz.
            try
            {
                await _api.AnswerCallbackAsync(cb.Id, toast, ct);
            }
            catch (TelegramApiException)
            {
                // Eskirgan callback (15 soniyadan ko'p) — muhim emas.
            }
        }
    }

    private async Task TrySendAsync(long chatId, string html, CancellationToken ct)
    {
        try
        {
            await _api.SendMessageAsync(chatId, html, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Xato xabarini ham yuborib bo'lmadi: chat {Chat}", chatId);
        }
    }
}
