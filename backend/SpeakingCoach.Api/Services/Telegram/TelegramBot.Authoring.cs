using System.Text;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// Bot orqali test qo'shish (/add, /bank, /cancel). Qoidalar — BotAuthoring'da
/// (testlanadi); bu yerda faqat baza, Telegram va fondagi ish.
/// </summary>
public partial class TelegramBot
{
    /// <summary>Fondagi ish uchun hamma narsa (webhook scope'i tugagandan keyin ham).</summary>
    private sealed record AuthorJob(long ChatId, Guid UserId, string Lang, bool IsAdmin, AuthorKind Kind, AuthorInput Input);

    private bool IsAdmin(Ctx c) => _admins.IsAdmin(c.User);

    /// <summary>Test qo'sha oladi: admin yoki kamida bitta guruhi bor o'qituvchi.</summary>
    private async Task<bool> CanAuthorAsync(Ctx c, CancellationToken ct) =>
        c.User is not null && (IsAdmin(c) || await _db.Groups.AnyAsync(g => g.TeacherId == c.User.Id, ct));

    private async Task<bool> RequireAuthorAsync(Ctx c, CancellationToken ct)
    {
        if (!await RequireLinkAsync(c, ct)) return false;
        if (await CanAuthorAsync(c, ct)) return true;
        await _api.SendMessageAsync(c.ChatId, c.T("bot.author_denied"), new[]
        {
            new[] { new TgButton(c.T("bot.open_site"), Url: $"{_options.FrontendUrl}/#/teacher") },
        }, ct: ct);
        return false;
    }

    private async Task StartAuthoringAsync(Ctx c, CancellationToken ct)
    {
        if (!await RequireAuthorAsync(c, ct)) return;
        await _api.SendMessageAsync(c.ChatId, c.T("bot.author_pick_exam"), BotAuthoring.ExamButtons(), ct: ct);
    }

    private async Task CancelAuthoringAsync(Ctx c, long? messageId, CancellationToken ct)
    {
        if (c.Account is { BotState: not null })
        {
            c.Account.BotState = null;
            await _db.SaveChangesAsync(ct);
        }
        if (messageId is not null) await _api.EditMessageAsync(c.ChatId, messageId.Value, c.T("bot.author_cancelled"), ct: ct);
        else await _api.SendMessageAsync(c.ChatId, c.T("bot.author_cancelled"), replyKeyboard: c.User is null ? null : BotLogic.MainMenu(c.Lang), ct: ct);
    }

    // ---------------- Material qabul qilish ----------------

    private async Task ReceiveAuthorInputAsync(Ctx c, TgMessage msg, AuthorKind kind, bool generate, CancellationToken ct)
    {
        if (c.User is null || !await CanAuthorAsync(c, ct))
        {
            c.Account!.BotState = null;
            await _db.SaveChangesAsync(ct);
            await _api.SendMessageAsync(c.ChatId, c.T("bot.author_denied"), ct: ct);
            return;
        }
        if (_runner.IsRunning(c.ChatId))
        {
            await _api.SendMessageAsync(c.ChatId, c.T("bot.author_busy"), ct: ct);
            return;
        }

        var (input, error, args) = BotAuthoring.ReadInput(msg, generate, kind);
        if (input is null)
        {
            await _api.SendMessageAsync(c.ChatId, c.T(error!, args), BotAuthoring.CancelButtons(c.Lang), ct: ct);
            return;
        }

        var isAdmin = IsAdmin(c);
        if (!isAdmin)
        {
            var since = DateTime.UtcNow.AddHours(-24);
            var userId = c.User.Id;
            var made = await _db.MockTests.CountAsync(t => t.CreatedByUserId == userId && t.Title != null && t.CreatedAtUtc > since, ct);
            if (made >= BotAuthoring.TeacherPerDay)
            {
                await _api.SendMessageAsync(c.ChatId, c.T("bot.author_limit", BotAuthoring.TeacherPerDay), ct: ct);
                return;
            }
        }

        var job = new AuthorJob(c.ChatId, c.User.Id, c.Lang, isAdmin, kind, input);
        var started = _runner.TryStart(c.ChatId, (sp, token) => sp.GetRequiredService<TelegramBot>().RunAuthoringAsync(job, sp.GetRequiredService<IMockAuthoring>(), token));
        await _api.SendMessageAsync(c.ChatId, c.T(started ? "bot.author_working" : "bot.author_busy"), ct: ct);
    }

    /// <summary>Fonda: material → Gemini → qoralama → muallifga ko'rib chiqish uchun yuborish.</summary>
    private async Task RunAuthoringAsync(AuthorJob job, IMockAuthoring authoring, CancellationToken ct)
    {
        string T(string key, params object?[] args) => Texts.Get(job.Lang, key, args);
        AuthoredTest test;
        try
        {
            byte[]? file = null;
            if (job.Input.FileId is { } fileId)
            {
                file = await _api.DownloadFileAsync(fileId, ct);
                if (file.Length > MockAuthoringRules.MaxFileBytes)
                {
                    await _api.SendMessageAsync(job.ChatId, T("bot.author_too_big", MockAuthoringRules.MaxFileBytes / (1024 * 1024)), ct: ct);
                    return;
                }
            }
            var source = new AuthorSource(job.Input.Text, file, job.Input.Mime, job.Input.Topic);
            test = await authoring.CreateAsync(job.Kind, source, ct);
        }
        catch (Exception ex)
        {
            // Holat saqlanadi — boshqa material yoki mavzu yuborish mumkin.
            _logger.LogWarning(ex, "Botda test tayyorlab bo'lmadi: {Kind}, chat {Chat}", job.Kind.Key, job.ChatId);
            await TrySendAsync(job.ChatId, $"{T("bot.author_failed")}\n\n<i>{BotLogic.Html(T("bot.author_reason", MockAuthoringRules.FailureReason(ex)))}</i>", CancellationToken.None);
            return;
        }

        var row = new MockTest
        {
            Id = Guid.NewGuid(),
            Exam = job.Kind.Exam,
            Module = job.Kind.Module,
            Variant = job.Kind.Variant,
            Payload = test.Payload,
            Title = Trim(test.Title, 200),
            Status = MockTestStatus.Draft,
            CreatedByUserId = job.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.MockTests.Add(row);
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(a => a.ChatId == job.ChatId, ct);
        if (account is not null && BotAuthoring.ParseState(account.BotState) is { } st && st.Kind == job.Kind) account.BotState = null;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Botda yangi qoralama: {Kind} {Id}", job.Kind.Key, row.Id);

        await SendTestAsync(job.ChatId, job.Lang, row, job.Kind,
            BotAuthoring.DraftMessage(job.Lang, job.Kind, row.Title!, MockAuthoringRules.Summary(job.Kind, row.Payload), job.IsAdmin),
            BotAuthoring.DraftButtons(job.Lang, row.Id, job.IsAdmin), ct);
    }

    private static string Trim(string s, int max) => s.Length > max ? s[..max] : s;

    /// <summary>Test haqida xabar + to'liq matn (.txt) — tugmalar fayl ostida.</summary>
    private async Task SendTestAsync(long chatId, string lang, MockTest row, AuthorKind kind, string html,
        IReadOnlyList<IReadOnlyList<TgButton>>? buttons, CancellationToken ct)
    {
        await _api.SendMessageAsync(chatId, html, ct: ct);
        var body = Encoding.UTF8.GetBytes(MockAuthoringRules.FullText(kind, row.Payload));
        await _api.SendDocumentAsync(chatId, BotAuthoring.FileName(kind, row.Id), body,
            $"{BotAuthoring.Emoji(kind.Module)} <b>{BotLogic.Html(row.Title ?? kind.Label)}</b> — <i>{BotAuthoring.StatusLabel(lang, row.Status)}</i>",
            buttons, ct);
    }

    // ---------------- Tugmalar ----------------

    private async Task<string?> HandleAuthorCallbackAsync(Ctx c, BotCallback action, long? messageId, CancellationToken ct)
    {
        switch (action)
        {
            case BotCallback.AuthorExam e:
                if (messageId is not null)
                {
                    await _api.EditMessageAsync(c.ChatId, messageId.Value,
                        c.T("bot.author_pick_kind", BotAuthoring.ExamLabel(e.Exam)), BotAuthoring.KindButtons(e.Exam), ct);
                }
                return null;

            case BotCallback.AuthorKindPick k when AuthorKind.Parse(k.Key) is { } kind && !BotAuthoring.NeedsMode(kind):
                return await PickModeAsync(c, kind, generate: false, messageId, ct);

            case BotCallback.AuthorKindPick k when AuthorKind.Parse(k.Key) is { } kind:
                if (messageId is not null)
                {
                    await _api.EditMessageAsync(c.ChatId, messageId.Value,
                        c.T("bot.author_pick_mode", BotLogic.Html(kind.Label)), BotAuthoring.ModeButtons(c.Lang, kind), ct);
                }
                return null;

            case BotCallback.AuthorModePick m when AuthorKind.Parse(m.Key) is { } kind:
                return await PickModeAsync(c, kind, m.Generate, messageId, ct);

            case BotCallback.AuthorCancel:
                await CancelAuthoringAsync(c, messageId, ct);
                return null;

            case BotCallback.TestCmd cmd:
                return await HandleTestCmdAsync(c, cmd, messageId, ct);
        }
        return null;
    }

    /// <summary>Tur va usul tanlandi — endi keyingi xabar (material, mavzu yoki havola) kutiladi.</summary>
    private async Task<string?> PickModeAsync(Ctx c, AuthorKind kind, bool generate, long? messageId, CancellationToken ct)
    {
        if (!await CanAuthorAsync(c, ct)) return c.T("bot.author_not_allowed");
        c.Account!.BotState = BotAuthoring.State(kind, generate);
        await _db.SaveChangesAsync(ct);
        var html = BotAuthoring.Instructions(c.Lang, kind, generate);
        if (messageId is not null) await _api.EditMessageAsync(c.ChatId, messageId.Value, html, BotAuthoring.CancelButtons(c.Lang), ct);
        else await _api.SendMessageAsync(c.ChatId, html, BotAuthoring.CancelButtons(c.Lang), ct: ct);
        return null;
    }

    private async Task<string?> HandleTestCmdAsync(Ctx c, BotCallback.TestCmd cmd, long? messageId, CancellationToken ct)
    {
        var row = await _db.MockTests.FirstOrDefaultAsync(t => t.Id == cmd.Id, ct);
        var isAdmin = IsAdmin(c);
        var isOwner = row is not null && row.CreatedByUserId == c.User!.Id;
        var next = row is null ? null : BotAuthoring.Transition(row.Status, cmd.Action, isAdmin, isOwner);
        if (row is null || next is null) return c.T("bot.author_not_allowed");
        var kind = BotAuthoring.KindOf(row);
        if (kind is null) return c.T("bot.author_not_allowed");

        if (cmd.Action == TestAction.View)
        {
            var buttons = row.Status switch
            {
                MockTestStatus.Pending when isAdmin => BotAuthoring.ReviewButtons(c.Lang, row.Id),
                MockTestStatus.Draft => BotAuthoring.DraftButtons(c.Lang, row.Id, isAdmin),
                _ => null,
            };
            await SendTestAsync(c.ChatId, c.Lang, row, kind,
                $"{BotAuthoring.Emoji(kind.Module)} <b>{BotLogic.Html(kind.Label)}</b>\n<code>{BotLogic.Html(MockAuthoringRules.Summary(kind, row.Payload))}</code>",
                buttons, ct);
            return null;
        }

        var previous = row.Status;
        row.Status = next;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Test {Id}: {From} → {To} ({Action})", row.Id, previous, next, cmd.Action);
        if (messageId is not null) await TryAsync(() => _api.EditButtonsAsync(c.ChatId, messageId.Value, null, ct));

        switch (cmd.Action)
        {
            case TestAction.Publish:
                await _api.SendMessageAsync(c.ChatId, c.T("bot.author_published"), ct: ct);
                if (!isOwner) await NotifyAuthorAsync(row, "bot.author_approved_note", ct);
                return c.T("bot.author_published");

            case TestAction.Submit:
                await _api.SendMessageAsync(c.ChatId, c.T("bot.author_submitted"), ct: ct);
                await NotifyAdminsAsync(c, row, kind, ct);
                return null;

            case TestAction.Reject:
                if (!isOwner) await NotifyAuthorAsync(row, "bot.author_rejected_note", ct);
                return c.T("bot.author_rejected");

            default:
                return c.T("bot.author_deleted");
        }
    }

    private async Task NotifyAuthorAsync(MockTest row, string key, CancellationToken ct)
    {
        if (row.CreatedByUserId is not { } authorId) return;
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(a => a.UserId == authorId, ct);
        if (account is null) return;
        await TrySendAsync(account.ChatId, Texts.Get(account.Lang, key, BotLogic.Html(row.Title ?? "")), ct);
    }

    /// <summary>O'qituvchi yubordi — botga ulangan har bir adminga ko'rib chiqish uchun.</summary>
    private async Task NotifyAdminsAsync(Ctx c, MockTest row, AuthorKind kind, CancellationToken ct)
    {
        var emails = _admins.Emails.ToList();
        if (emails.Count == 0) return;
        var admins = await (from u in _db.Users
                            join a in _db.TelegramAccounts on u.Id equals a.UserId
                            where emails.Contains(u.Email)
                            select new { a.ChatId, a.Lang }).ToListAsync(ct);
        foreach (var admin in admins)
        {
            var html = BotAuthoring.ReviewMessage(admin.Lang, c.User!.Email, kind, row.Title ?? kind.Label, MockAuthoringRules.Summary(kind, row.Payload));
            await TryAsync(() => SendTestAsync(admin.ChatId, admin.Lang, row, kind, html, BotAuthoring.ReviewButtons(admin.Lang, row.Id), ct));
        }
    }

    // ---------------- /bank ----------------

    private async Task SendBankAsync(Ctx c, CancellationToken ct)
    {
        if (!await RequireAuthorAsync(c, ct)) return;

        if (IsAdmin(c))
        {
            var counts = (await _db.MockTests
                    .Where(t => t.Status == MockTestStatus.Published || t.Status == MockTestStatus.Pending)
                    .GroupBy(t => new { t.Exam, t.Module, t.Variant, t.Status })
                    .Select(g => new { g.Key.Exam, g.Key.Module, g.Key.Variant, g.Key.Status, Count = g.Count() })
                    .ToListAsync(ct))
                .GroupBy(x => $"{x.Exam}:{x.Module}:{x.Variant}")
                .ToDictionary(
                    g => g.Key,
                    g => (g.Where(x => x.Status == MockTestStatus.Published).Sum(x => x.Count), g.Where(x => x.Status == MockTestStatus.Pending).Sum(x => x.Count)));
            var pending = await _db.MockTests
                .Where(t => t.Status == MockTestStatus.Pending)
                .OrderBy(t => t.CreatedAtUtc)
                .Take(10)
                .Select(t => new { t.Id, t.Title, t.Module })
                .ToListAsync(ct);
            await _api.SendMessageAsync(c.ChatId, BotAuthoring.BankText(c.Lang, counts), ViewButtons(c, pending.Select(p => (p.Id, p.Title, p.Module))), ct: ct);
            return;
        }

        var userId = c.User!.Id;
        var mine = await _db.MockTests
            .Where(t => t.CreatedByUserId == userId && t.Title != null && t.Status != MockTestStatus.Deleted)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(10)
            .ToListAsync(ct);
        var list = mine.Select(t => (Row: t, Kind: BotAuthoring.KindOf(t))).Where(x => x.Kind is not null).ToList();
        await _api.SendMessageAsync(c.ChatId,
            BotAuthoring.MineText(c.Lang, list.Select(x => (x.Kind!, x.Row.Title!, x.Row.Status)))
                + "\n\n<i>" + c.T("bot.not_admin_hint", BotLogic.Html(BotLogic.MaskEmail(c.User.Email))) + "</i>",
            ViewButtons(c, list.Select(x => (x.Row.Id, x.Row.Title, x.Row.Module))), ct: ct);
    }

    private static IReadOnlyList<IReadOnlyList<TgButton>>? ViewButtons(Ctx c, IEnumerable<(Guid Id, string? Title, string Module)> tests)
    {
        var rows = tests
            .Select(t => (IReadOnlyList<TgButton>)new[] { new TgButton($"{BotAuthoring.Emoji(t.Module)} {Trim(t.Title ?? c.T("bot.author_view"), 50)}", BotLogic.Encode(new BotCallback.TestCmd(TestAction.View, t.Id))) })
            .ToList();
        return rows.Count == 0 ? null : rows;
    }

    private async Task TryAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram amali bajarilmadi (muhim emas)");
        }
    }
}
