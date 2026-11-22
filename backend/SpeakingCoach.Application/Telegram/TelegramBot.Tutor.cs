using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Application.Telegram;

/// <summary>
/// AI ustoz botda (/dars): mavzu yozasiz → tushuntirish → mashqlar (tugmalar) → o'z gaplari → mini-test → natija.
/// Mantiq va holat saytdagi bilan bir xil (TutorService): bu yerda faqat Telegram ko'rinishi.
/// Matn kutilayotganda (mavzu, o'z gaplari, savol) holat TelegramAccount.BotState'da ("tu:..."); /cancel — bekor.
/// </summary>
public partial class TelegramBot
{
    private async Task SetBotStateAsync(Ctx c, string? state, CancellationToken ct)
    {
        if (c.Account is null || c.Account.BotState == state) return;
        c.Account.BotState = state;
        await _db.SaveChangesAsync(ct);
    }

    private static string AfterCommand(string text)
    {
        var i = text.IndexOf(' ');
        return i < 0 ? "" : text[(i + 1)..].Trim();
    }

    private Task SendPlainAsync(Ctx c, string key, object?[] args, CancellationToken ct) =>
        _api.SendMessageAsync(c.ChatId, c.T(key, args), ct: ct);

    /// <summary>/dars [mavzu]: mavzu bo'lsa darhol boshlanadi, bo'lmasa — mavzu so'raladi.</summary>
    private async Task TutorEntryAsync(Ctx c, string text, CancellationToken ct)
    {
        if (!await RequireLinkAsync(c, ct)) return;
        if (!_flags.AiTutor)
        {
            await SendPlainAsync(c, "tutor.disabled", [], ct);
            return;
        }
        var topic = AfterCommand(text);
        if (topic.Length > 0)
        {
            await StartTutorLessonAsync(c, topic, ct);
            return;
        }
        await AskTutorTopicAsync(c, ct);
    }

    private async Task AskTutorTopicAsync(Ctx c, CancellationToken ct)
    {
        await SetBotStateAsync(c, BotTutor.State(TutorAwait.Topic), ct);
        // Xatolar asosidagi tavsiya (AI'siz); xato bo'lsa ham dars tanlash to'xtamasin.
        var recommended = new List<string>();
        try
        {
            if (c.User is not null) recommended = [.. (await _tutor.RecommendAsync(c.User, TopicAdvisor.DefaultMax, ct)).Select(r => r.Topic)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AI ustoz tavsiyasi olinmadi: chat {Chat}", c.ChatId);
        }
        var html = c.T("tutor.bot_ask_topic") + (recommended.Count > 0 ? "\n\n" + c.T("tutor.bot_recommend") : "");
        await _api.SendMessageAsync(c.ChatId, html, BotTutor.TopicButtons(recommended), ct: ct);
    }

    /// <summary>Matn kutilayotganda kelgan xabar (buyruq emas) — mavzu, o'z gaplari yoki savol.</summary>
    private async Task ReceiveTutorTextAsync(Ctx c, string text, TutorAwait kind, Guid? lessonId, CancellationToken ct)
    {
        if (c.User is null || !_flags.AiTutor)
        {
            await SetBotStateAsync(c, null, ct);
            await SendPlainAsync(c, c.User is null ? "bot.need_link" : "tutor.disabled", [], ct);
            return;
        }
        switch (kind)
        {
            case TutorAwait.Topic:
                await StartTutorLessonAsync(c, text, ct);
                break;
            case TutorAwait.Write when lessonId is { } wid:
                await SendPlainAsync(c, "tutor.bot_checking", [], ct);
                await RunTutorAiAsync(c, ct, async () =>
                {
                    var r = await _tutor.WriteAsync(wid, c.User, text, ct);
                    if (!r.Ok)
                    {
                        await SendPlainAsync(c, r.Fail!.Key, r.Fail.Args, ct);
                        if (r.Fail.Key is "tutor.not_found" or "tutor.locked") await SetBotStateAsync(c, null, ct);
                        return;
                    }
                    await SetBotStateAsync(c, null, ct);
                    await _api.SendMessageAsync(c.ChatId, BotTutor.WritingText(c.Lang, r.Value!),
                        [[new TgButton(c.T("tutor.bot_skip_btn"), BotLogic.Encode(new BotCallback.Tutor(wid, "s")))]], ct: ct);
                });
                break;
            case TutorAwait.Ask when lessonId is { } qid:
                await RunTutorAiAsync(c, ct, async () =>
                {
                    var r = await _tutor.AskAsync(qid, c.User, text, ct);
                    if (!r.Ok)
                    {
                        await SendPlainAsync(c, r.Fail!.Key, r.Fail.Args, ct);
                        if (r.Fail.Key is "tutor.not_found" or "tutor.asks_limit") await SetBotStateAsync(c, null, ct);
                        return;
                    }
                    await SetBotStateAsync(c, null, ct);
                    var left = r.Value!.AsksLeft;
                    var html = $"{BotLogic.Html(r.Value.Answer)}\n\n<i>{c.T("tutor.bot_asks_left", left)}</i>";
                    var row = new List<TgButton> { new(c.T("tutor.bot_continue"), BotLogic.Encode(new BotCallback.Tutor(qid, "g"))) };
                    if (left > 0) row.Add(new TgButton(c.T("tutor.bot_ask_btn"), BotLogic.Encode(new BotCallback.Tutor(qid, "q"))));
                    await _api.SendMessageAsync(c.ChatId, html, [row], ct: ct);
                });
                break;
            default:
                await SetBotStateAsync(c, null, ct);
                break;
        }
    }

    /// <summary>AI chaqiradigan amal: AI xatosi — tushunarli xabar (AiBusy esa umumiy qayta ishlovchiga ketadi).</summary>
    private async Task RunTutorAiAsync(Ctx c, CancellationToken ct, Func<Task> run)
    {
        try
        {
            await run();
        }
        catch (Exception ex) when (ex is not AiBusyException and not OperationCanceledException and not TelegramApiException)
        {
            _logger.LogError(ex, "AI ustoz (bot) amali bajarilmadi: chat {Chat}", c.ChatId);
            await SendPlainAsync(c, "ai_unavailable", [], ct);
        }
    }

    private async Task StartTutorLessonAsync(Ctx c, string topic, CancellationToken ct, Guid? assignmentId = null, string source = "free")
    {
        if (Tutor.NormalizeTopic(topic) is null)
        {
            await SendPlainAsync(c, "tutor.bad_topic", [], ct);
            return;
        }
        var guestKey = $"tg:{c.ChatId}";
        var denied = await _quotas.CheckAsync(c.User, guestKey, AiKind.Exercise);
        if (denied is not null)
        {
            await _api.SendMessageAsync(c.ChatId, c.T(denied.Value.Key, denied.Value.Args), ct: ct);
            return;
        }
        await SendPlainAsync(c, "tutor.bot_preparing", [], ct);
        await RunTutorAiAsync(c, ct, async () =>
        {
            var r = await _tutor.StartAsync(c.User!, topic, null, c.Lang, ct, assignmentId is not null, source);
            if (!r.Ok)
            {
                // Mavzudan tashqari bo'lsa — holat saqlanadi: foydalanuvchi boshqa mavzu yozishi mumkin.
                await SendPlainAsync(c, r.Fail!.Key, r.Fail.Args, ct);
                return;
            }
            await _quotas.RecordAsync(c.User, guestKey, AiKind.Exercise);
            await SetBotStateAsync(c, null, ct);
            var id = r.Value!.LessonId;
            await _api.SendMessageAsync(c.ChatId, BotTutor.ExplainText(c.Lang, r.Value.State),
            [
                [new TgButton(c.T("tutor.bot_to_practice"), BotLogic.Encode(new BotCallback.Tutor(id, "g")))],
                [new TgButton(c.T("tutor.bot_ask_btn"), BotLogic.Encode(new BotCallback.Tutor(id, "q")))],
            ], ct: ct);
        });
    }

    /// <summary>Tugma bosildi. Qaytariladigan qiymat — kichik "toast" (xato bo'lsa).</summary>
    private async Task<string?> HandleTutorCallbackAsync(Ctx c, BotCallback.Tutor cb, long? messageId, CancellationToken ct)
    {
        if (!_flags.AiTutor) return c.T("tutor.disabled");
        var user = c.User!;

        if (cb.Op == "t")
        {
            if (cb.Arg == "new")
            {
                await AskTutorTopicAsync(c, ct);
                return null;
            }
            // Ustoz vazifasi: mavzu vazifadan olinadi (guruh a'zoligi tekshiriladi).
            if (BotTutor.ParseAssignmentArg(cb.Arg) is { } assignmentId)
            {
                var resolved = await _tutor.ResolveAssignmentTopicAsync(assignmentId, user);
                if (!resolved.Ok) return c.T(resolved.Fail!.Key, resolved.Fail.Args);
                await StartTutorLessonAsync(c, resolved.Value!, ct, assignmentId);
                return null;
            }
            // Boshqa mavzu so'rovi ochiq bo'lsa ham, tugma bosilsa — tanlangan mavzu bilan boshlaymiz.
            if (BotTutor.ParseTopicArg(cb.Arg) is not { } pick) return c.T("tutor.not_found");
            await StartTutorLessonAsync(c, Tutor.AllTopics[pick.Index], ct, source: pick.Recommended ? "recommended" : "free");
            return null;
        }

        // Dars bahosi (natija xabaridan): mavjud baholar jadvaliga; ikki marta bosilmasin — baho qatori olib tashlanadi.
        if (cb.Op == "r")
        {
            _db.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Kind = FeedbackKinds.Rating,
                Message = cb.Arg == "u" ? TutorStats.RatingUp : TutorStats.RatingDown,
                Page = "#/tutor",
                Lang = c.Lang,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
            if (messageId is not null) await _api.EditButtonsAsync(c.ChatId, messageId.Value, BotTutor.ResultButtons(c.Lang, withRating: false), ct);
            return c.T("tutor.bot_thanks");
        }

        var id = cb.Id!.Value;
        switch (cb.Op)
        {
            case "a":
            {
                var (section, index, option) = BotTutor.ParseAnswerArg(cb.Arg);
                var r = await _tutor.AnswerAsync(id, user, section, index, option);
                if (!r.Ok) return c.T(r.Fail!.Key, r.Fail.Args);
                var st = await _tutor.GetAsync(id, user);
                if (!st.Ok) return c.T("tutor.not_found");
                // Savol xabari natija bilan almashtiriladi (tugmalar o'rniga "Keyingisi").
                if (messageId is not null)
                    await _api.EditMessageAsync(c.ChatId, messageId.Value, BotTutor.AnsweredText(c.Lang, st.Value!, section, index),
                        [[new TgButton(c.T("tutor.bot_next"), BotLogic.Encode(new BotCallback.Tutor(id, "g")))]], ct);
                return null;
            }

            case "g":
                return await TutorNextAsync(c, id, ct);

            case "s":
            {
                var st = await _tutor.GetAsync(id, user);
                if (!st.Ok) return c.T("tutor.not_found");
                if (!Tutor.PracticeDone(st.Value!)) return c.T("tutor.locked");
                await SendTutorQuestionAsync(c, id, st.Value!, "test", 0, ct);
                return null;
            }

            case "w":
            {
                var st = await _tutor.GetAsync(id, user);
                if (!st.Ok) return c.T("tutor.not_found");
                if (!Tutor.CanWrite(st.Value!)) return c.T("tutor.locked");
                await SetBotStateAsync(c, BotTutor.State(TutorAwait.Write, id), ct);
                await _api.SendMessageAsync(c.ChatId, c.T("tutor.bot_write_prompt", BotLogic.Html(st.Value!.WritingTask)), ct: ct);
                return null;
            }

            case "q":
            {
                var st = await _tutor.GetAsync(id, user);
                if (!st.Ok) return c.T("tutor.not_found");
                if (!Tutor.CanAsk(st.Value!)) return c.T("tutor.asks_limit", Tutor.MaxAsksPerLesson);
                await SetBotStateAsync(c, BotTutor.State(TutorAwait.Ask, id), ct);
                await _api.SendMessageAsync(c.ChatId, c.T("tutor.bot_ask_prompt", Tutor.MaxAsksPerLesson - st.Value!.Asks), ct: ct);
                return null;
            }
        }
        return null;
    }

    /// <summary>Navbatdagi qadam: savol, yozma topshiriq taklifi yoki natija (mini-test tugagach).</summary>
    private async Task<string?> TutorNextAsync(Ctx c, Guid id, CancellationToken ct)
    {
        var st = await _tutor.GetAsync(id, c.User!);
        if (!st.Ok) return c.T("tutor.not_found");
        var state = st.Value!;
        var next = BotTutor.NextStep(state);
        switch (next.Kind)
        {
            case "question":
                await SendTutorQuestionAsync(c, id, state, next.Section, next.Index, ct);
                return null;

            case "write":
                await _api.SendMessageAsync(c.ChatId, c.T("tutor.bot_write_prompt", BotLogic.Html(state.WritingTask)),
                [
                    [new TgButton(c.T("tutor.bot_write_btn"), BotLogic.Encode(new BotCallback.Tutor(id, "w")))],
                    [new TgButton(c.T("tutor.bot_skip_btn"), BotLogic.Encode(new BotCallback.Tutor(id, "s")))],
                ], ct: ct);
                return null;

            default:
            {
                var f = await _tutor.FinishAsync(id, c.User!);
                if (!f.Ok) return c.T(f.Fail!.Key, f.Fail.Args);
                await _api.SendMessageAsync(c.ChatId, BotTutor.ResultText(c.Lang, f.Value!), BotTutor.ResultButtons(c.Lang, withRating: true), ct: ct);
                return null;
            }
        }
    }

    private Task SendTutorQuestionAsync(Ctx c, Guid id, TutorState state, string section, int index, CancellationToken ct)
    {
        var list = section == "test" ? state.Test : state.Practice;
        return _api.SendMessageAsync(c.ChatId, BotTutor.QuestionText(c.Lang, state, section, index),
            BotTutor.OptionButtons(id, section, index, list[index]), ct: ct);
    }
}
