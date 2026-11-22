using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Application.Telegram;

/// <summary>
/// Shaxsiy podcast (/podcast): istalgan foydalanuvchi YouTube havolasini yuboradi — Gemini uni gaplarga bo'lib shadowing
/// darsi qiladi (faqat egasiga ko'rinadi); mashq Telegram ichida (Mini App) ochiladi.
/// Tayyorlash 1–3 daqiqa: fonda (AuthoringRunner, chatda bir vaqtda bitta ish), kunlik limit Ai:PodcastsPerDay.
/// </summary>
public partial class TelegramBot
{
    private sealed record PodcastJob(long ChatId, Guid UserId, string Lang, AuthorInput Input);

    /// <summary>/podcast: ko'rsatma, mavjud videolar (tugmalar bilan) va havola kutish holati.</summary>
    private async Task PodcastEntryAsync(Ctx c, CancellationToken ct)
    {
        if (!await RequireLinkAsync(c, ct)) return;
        if (!_flags.PersonalPodcasts)
        {
            await SendPlainAsync(c, "pc.disabled", [], ct);
            return;
        }
        var mine = await _podcasts.ListAsync(c.User!.Id, 8, ct);
        await SetBotStateAsync(c, BotPodcast.AwaitLink, ct);
        await _api.SendMessageAsync(c.ChatId, BotPodcast.IntroText(c.Lang, _podcasts.PerDay, PersonalShadowingService.MaxStored, mine.Count > 0),
            BotAuthoring.CancelButtons(c.Lang), ct: ct);
        foreach (var (id, title, lines) in mine)
        {
            await _api.SendMessageAsync(c.ChatId, $"🎬 <b>{BotLogic.Html(title)}</b> · {lines}",
                PodcastButtons(c, id), ct: ct);
        }
    }

    private IReadOnlyList<IReadOnlyList<TgButton>> PodcastButtons(Ctx c, Guid id) =>
    [
        [
            _options.SiteButton(c.T("pc.practice_btn"), BotPodcast.PracticeRoute(id)),
            new TgButton(c.T("pc.delete_btn"), BotLogic.Encode(new BotCallback.PodcastDelete(id))),
        ],
    ];

    /// <summary>Havola kutilayotganda kelgan xabar.</summary>
    private async Task ReceivePodcastInputAsync(Ctx c, TgMessage msg, CancellationToken ct)
    {
        if (c.User is null || !_flags.PersonalPodcasts)
        {
            await SetBotStateAsync(c, null, ct);
            await SendPlainAsync(c, c.User is null ? "bot.need_link" : "pc.disabled", [], ct);
            return;
        }
        if (_runner.IsRunning(c.ChatId))
        {
            await SendPlainAsync(c, "pc.busy", [], ct);
            return;
        }
        // Havola va vaqt oralig'ini tekshirish — mavjud YouTube shadowing qoidalari (xato bo'lsa, holat saqlanadi).
        var (input, error, args) = BotAuthoring.ReadInput(msg, false, PersonalShadowingService.Kind);
        if (input is null)
        {
            await _api.SendMessageAsync(c.ChatId, c.T(error!, args), BotAuthoring.CancelButtons(c.Lang), ct: ct);
            return;
        }
        if (await _podcasts.CountAsync(c.User.Id, ct) >= PersonalShadowingService.MaxStored)
        {
            await SendPlainAsync(c, "pc.full", [PersonalShadowingService.MaxStored], ct);
            return;
        }
        if (!_podcasts.TryTakeAttempt(c.User))
        {
            await SendPlainAsync(c, "pc.limit", [_podcasts.PerDay], ct);
            return;
        }

        var job = new PodcastJob(c.ChatId, c.User.Id, c.Lang, input);
        var started = _runner.TryStart(c.ChatId, (sp, token) => sp.GetRequiredService<TelegramBot>().RunPodcastAsync(job, sp.GetRequiredService<IMockAuthoring>(), token));
        if (!started) _podcasts.RefundAttempt(c.User);
        await SendPlainAsync(c, started ? "pc.working" : "pc.busy", [], ct);
    }

    /// <summary>Fonda: havola → Gemini → shaxsiy dars → "Mashq qilish" tugmasi.</summary>
    private async Task RunPodcastAsync(PodcastJob job, IMockAuthoring authoring, CancellationToken ct)
    {
        string T(string key, params object?[] args) => Texts.Get(job.Lang, key, args);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == job.UserId, ct);
        if (user is null) return;

        AuthoredTest test;
        try
        {
            test = await authoring.CreateAsync(PersonalShadowingService.Kind, new AuthorSource(job.Input.Text, null, null, null), ct);
        }
        catch (AiBusyException ex)
        {
            _podcasts.RefundAttempt(user);
            _logger.LogWarning("Shaxsiy podcast tayyorlanmadi — AI band ({Reason}): chat {Chat}", ex.Reason, job.ChatId);
            await TrySendAsync(job.ChatId, T("ai_busy"), CancellationToken.None);
            return;
        }
        catch (Exception ex)
        {
            // Urinish qaytariladi; holat saqlanadi — boshqa havola yuborish mumkin.
            _podcasts.RefundAttempt(user);
            _logger.LogWarning(ex, "Shaxsiy podcast tayyorlab bo'lmadi: chat {Chat}", job.ChatId);
            await TrySendAsync(job.ChatId, $"{T("bot.author_failed")}\n\n<i>{BotLogic.Html(T("bot.author_reason", MockAuthoringRules.FailureReason(ex)))}</i>", CancellationToken.None);
            return;
        }

        var row = await _podcasts.SaveAsync(job.UserId, test, ct);
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(a => a.ChatId == job.ChatId, ct);
        if (account is not null && BotPodcast.IsAwaitingLink(account.BotState))
        {
            account.BotState = null;
            await _db.SaveChangesAsync(ct);
        }
        var d = BotPodcast.Describe(row.Title, row.Payload);
        var ctx = new Ctx(job.ChatId, new TgUser(job.ChatId, null, null, job.Lang), account, user, job.Lang);
        await _api.SendMessageAsync(job.ChatId, BotPodcast.ReadyText(job.Lang, d?.Title ?? row.Title ?? "", d?.Lines ?? 0, d?.Seconds ?? 0), PodcastButtons(ctx, row.Id), ct: ct);
    }

    private async Task<string?> HandlePodcastDeleteAsync(Ctx c, BotCallback.PodcastDelete cb, long? messageId, CancellationToken ct)
    {
        if (!await _podcasts.DeleteAsync(c.User!.Id, cb.Id, ct)) return c.T("pc.not_found");
        if (messageId is not null) await _api.EditMessageAsync(c.ChatId, messageId.Value, c.T("pc.deleted"), ct: ct);
        return null;
    }
}
