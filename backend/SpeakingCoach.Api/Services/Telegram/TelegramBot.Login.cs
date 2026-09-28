using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// Saytga Telegram orqali kirish (bot tomoni): /start login_NONCE → raqamli
/// tugmalar; to'g'ri raqam → tasdiqlandi (hisob topiladi yoki ochiladi).
/// Qoidalar — <see cref="TelegramLoginRules"/>.
/// </summary>
public partial class TelegramBot
{
    /// <summary>/start login_NONCE: urinish shu chatga bog'lanadi va saytdagi raqamni yozish so'raladi.</summary>
    private async Task StartWebLoginAsync(Ctx c, TgChat chat, string nonce, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var hash = TelegramLoginRules.Hash(nonce);
        var chatId = c.ChatId;

        // Faqat shaxsiy chat: guruhda havola "egallanib", hech qachon tasdiqlanmay qolmasin.
        if (chat.Type != "private" || chatId != c.From.Id)
        {
            await _api.SendMessageAsync(chatId, c.T("tglogin.private_only"), ct: ct);
            return;
        }

        // Havolani birinchi ochgan chat "egallaydi" (atomar): boshqa chatga
        // yuborilgan havola bilan birovning urinishini tasdiqlab bo'lmaydi.
        await _db.TelegramLogins
            .Where(l => l.NonceHash == hash && l.ChatId == null && l.ConfirmedAtUtc == null && l.RejectedAtUtc == null && l.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(set => set.SetProperty(l => l.ChatId, (long?)chatId), ct);

        var login = await _db.TelegramLogins.AsNoTracking().FirstOrDefaultAsync(l => l.NonceHash == hash, ct);
        if (login is null || login.ChatId != chatId || !TelegramLoginRules.IsOpen(login, now))
        {
            await _api.SendMessageAsync(chatId, c.T("tglogin.expired"), ct: ct);
            return;
        }

        // Raqamni tanlash emas, YOZISH: tasodifan to'g'ri bosish ehtimoli 1/3 emas, 1/90;
        // foydalanuvchi saytga qarashga majbur (Microsoft Authenticator'dagi kabi).
        await _api.SendMessageAsync(chatId, c.T("tglogin.prompt"), new[]
        {
            new[] { new TgButton(c.T("tglogin.not_me"), BotLogic.Encode(new BotCallback.WebLogin(login.Id, null))) },
        }, ct: ct);
    }

    /// <summary>
    /// Oddiy xabar 2 xonali raqam bo'lsa va shu chatda kutilayotgan kirish urinishi bo'lsa —
    /// uni tasdiqlaydi yoki (noto'g'ri raqam) bekor qiladi. true — xabar shu yerda qayta ishlandi.
    /// </summary>
    private async Task<bool> TryTypedWebLoginAsync(Ctx c, TgChat chat, string text, CancellationToken ct)
    {
        if (chat.Type != "private" || !TelegramLoginRules.IsTypedCode(text)) return false;
        var now = DateTime.UtcNow;
        var chatId = c.ChatId;
        var pending = await _db.TelegramLogins.AsNoTracking()
            .Where(l => l.ChatId == chatId && l.ConfirmedAtUtc == null && l.RejectedAtUtc == null && l.ExpiresAtUtc > now)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(ct);
        if (pending is null) return false;
        var number = int.Parse(text.Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var reply = await ConfirmWebLoginAsync(c, c.From, pending.Value, number, null, ct);
        await _api.SendMessageAsync(chatId, reply, ct: ct);
        return true;
    }

    /// <summary>"Bu men emasman" tugmasi (yoki eski xabardagi raqam tugmasi). Qaytaradi — callback javobi (toast).</summary>
    private async Task<string> HandleWebLoginAsync(Ctx c, TgCallbackQuery cb, BotCallback.WebLogin action, long? messageId, CancellationToken ct)
    {
        // Faqat shaxsiy chat (u yerda chat ID = foydalanuvchi ID).
        if (cb.Message?.Chat.Type != "private" || c.ChatId != cb.From.Id) return c.T("tglogin.expired");
        return await ConfirmWebLoginAsync(c, cb.From, action.LoginId, action.Number, messageId, ct);
    }

    /// <summary>To'g'ri raqam — tasdiq (hisob topiladi yoki ochiladi); noto'g'ri yoki null — bekor.</summary>
    private async Task<string> ConfirmWebLoginAsync(Ctx c, TgUser from, Guid id, int? number, long? messageId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var chatId = c.ChatId;

        async Task<string> Finish(string key)
        {
            var text = c.T(key);
            if (messageId is not null) await _api.EditMessageAsync(chatId, messageId.Value, text, ct: ct);
            return text;
        }

        var login = await _db.TelegramLogins.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (login is null || login.ChatId != chatId || !TelegramLoginRules.IsOpen(login, now))
        {
            return await Finish("tglogin.expired");
        }

        if (!TelegramLoginRules.Matches(login.Code, number))
        {
            // Noto'g'ri raqam yoki "Bu men emasman" — urinish bekor (qayta taxmin qilib bo'lmaydi).
            await _db.TelegramLogins
                .Where(l => l.Id == id && l.ConfirmedAtUtc == null && l.RejectedAtUtc == null)
                .ExecuteUpdateAsync(set => set.SetProperty(l => l.RejectedAtUtc, (DateTime?)now), ct);
            return await Finish("tglogin.cancelled");
        }

        // Atomar tasdiq: parallel ikki javobdan faqat bittasi o'tadi.
        var username = TelegramLoginRules.Clip(from.Username, 64);
        var firstName = TelegramLoginRules.Clip(from.FirstName, 64);
        var confirmed = await _db.TelegramLogins
            .Where(l => l.Id == id && l.ChatId == chatId && l.ConfirmedAtUtc == null && l.RejectedAtUtc == null && l.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(set => set
                .SetProperty(l => l.ConfirmedAtUtc, (DateTime?)now)
                .SetProperty(l => l.TgUsername, username)
                .SetProperty(l => l.TgFirstName, firstName), ct);
        if (confirmed == 0) return await Finish("tglogin.expired");

        var wasLinked = c.User is not null;
        var (user, isNew) = await _auth.ResolveTelegramUserAsync(from, SignupMethods.Telegram, login.Lang, ct);
        var userId = user.Id;
        await _db.TelegramLogins
            .Where(l => l.Id == id)
            .ExecuteUpdateAsync(set => set
                .SetProperty(l => l.UserId, (Guid?)userId)
                .SetProperty(l => l.IsNewUser, isNew), ct);
        _logger.LogInformation("Telegram orqali saytga kirish tasdiqlandi: {UserId} (yangi: {IsNew})", userId, isNew);

        var done = await Finish("tglogin.done");
        if (!wasLinked)
        {
            // Chat endi hisobga ulandi — pastdagi menyu.
            var lang = TelegramLoginRules.NormalizeLang(login.Lang);
            await _api.SendMessageAsync(chatId, Texts.Get(lang, "bot.help"), replyKeyboard: BotLogic.MainMenu(lang), ct: ct);
        }
        return done;
    }
}
