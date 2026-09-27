using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>Qoralama/test ustidagi amal: chop etish, tekshiruvga yuborish, rad etish, o'chirish, ko'rish.</summary>
public enum TestAction
{
    Publish,
    Submit,
    Reject,
    Delete,
    View,
}

/// <summary>Botga kelgan material: matn, fayl (Telegram file_id) yoki mavzu.</summary>
public record AuthorInput(string? Text, string? FileId, string? Mime, string? Topic);

/// <summary>
/// Bot orqali test qo'shish — toza qoidalar: bot holati, tugmalar, kim
/// qaysi amalni bajara oladi. Bazaga va tarmoqqa tegmaydi (testlanadi).
///
/// Oqim: /add → imtihon → bo'lim (→ variant) → "material yuboraman" yoki
/// "mavzu, Gemini yaratsin" → keyingi xabar (matn / PDF / rasm) → qoralama
/// (to'liq matn .txt bilan) → admin: ✅ chop etish; o'qituvchi: 📨 adminga
/// → admin ✅ tasdiqlaydi yoki ❌ rad etadi → muallifga xabar.
/// </summary>
public static class BotAuthoring
{
    /// <summary>O'qituvchi uchun sutkalik chegara (Gemini kvotasi); admin — cheksiz.</summary>
    public const int TeacherPerDay = 10;

    private const string StatePrefix = "au:";

    public static string State(AuthorKind kind, bool generate) => $"{StatePrefix}{kind.Key}:{(generate ? "gen" : "src")}";

    public static (AuthorKind Kind, bool Generate)? ParseState(string? state)
    {
        if (state is null || !state.StartsWith(StatePrefix, StringComparison.Ordinal)) return null;
        var rest = state[StatePrefix.Length..];
        var i = rest.LastIndexOf(':');
        if (i < 0) return null;
        var mode = rest[(i + 1)..];
        if (mode is not ("gen" or "src")) return null;
        return AuthorKind.Parse(rest[..i]) is { } kind ? (kind, mode == "gen") : null;
    }

    /// <summary>
    /// Holat o'tishi: yangi holat yoki null (ruxsat yo'q / bu holatda mumkin emas).
    /// Admin: qoralama yoki tekshiruvdagini chop etadi, tekshiruvdagini rad etadi.
    /// Muallif (o'qituvchi): qoralamasini tekshiruvga yuboradi.
    /// O'chirish: muallif — chop etilmaganini; admin — istalganini (yomon testni bankdan olish).
    /// </summary>
    public static string? Transition(string status, TestAction action, bool isAdmin, bool isOwner) => action switch
    {
        TestAction.Publish when isAdmin && status is MockTestStatus.Draft or MockTestStatus.Pending => MockTestStatus.Published,
        TestAction.Reject when isAdmin && status == MockTestStatus.Pending => MockTestStatus.Rejected,
        TestAction.Submit when isOwner && !isAdmin && status == MockTestStatus.Draft => MockTestStatus.Pending,
        TestAction.Delete when status != MockTestStatus.Deleted && (isAdmin || (isOwner && status != MockTestStatus.Published)) => MockTestStatus.Deleted,
        TestAction.View when (isAdmin || isOwner) && status != MockTestStatus.Deleted => status,
        _ => null,
    };

    public static char Code(TestAction a) => a switch
    {
        TestAction.Publish => 'p',
        TestAction.Submit => 's',
        TestAction.Reject => 'r',
        TestAction.Delete => 'd',
        _ => 'v',
    };

    public static TestAction? FromCode(string code) => code switch
    {
        "p" => TestAction.Publish,
        "s" => TestAction.Submit,
        "r" => TestAction.Reject,
        "d" => TestAction.Delete,
        "v" => TestAction.View,
        _ => null,
    };

    public static readonly string[] Exams = ["ielts", "cefr", ShadowingService.Exam];

    public static string ExamLabel(string exam) => exam switch
    {
        "cefr" => "CEFR",
        ShadowingService.Exam => "Shadowing",
        _ => "IELTS",
    };

    /// <summary>YouTube darsi uchun "usul" so'ralmaydi — faqat havola.</summary>
    public static bool NeedsMode(AuthorKind kind) => !(kind.IsShadowing && kind.Module == ShadowingRules.YouTube);

    /// <summary>Tanlangan imtihon uchun turlar (bo'lim + variant).</summary>
    public static IEnumerable<AuthorKind> KindsOf(string exam) => AuthorKind.All.Where(k => k.Exam == exam);

    public static IReadOnlyList<IReadOnlyList<TgButton>> ExamButtons() =>
    [
        [new TgButton("🎓 IELTS", BotLogic.Encode(new BotCallback.AuthorExam("ielts"))), new TgButton("🇺🇿 CEFR", BotLogic.Encode(new BotCallback.AuthorExam("cefr")))],
        [new TgButton("🎬 Shadowing", BotLogic.Encode(new BotCallback.AuthorExam(ShadowingService.Exam)))],
    ];

    public static IReadOnlyList<IReadOnlyList<TgButton>> KindButtons(string exam) =>
        KindsOf(exam).Chunk(2)
            .Select(row => (IReadOnlyList<TgButton>)row.Select(k => new TgButton(Emoji(k.Module) + " " + k.ModuleLabel, BotLogic.Encode(new BotCallback.AuthorKindPick(k.Key)))).ToArray())
            .ToList();

    public static IReadOnlyList<IReadOnlyList<TgButton>> ModeButtons(string lang, AuthorKind kind) =>
    [
        [new TgButton(Texts.Get(lang, "bot.author_mode_src"), BotLogic.Encode(new BotCallback.AuthorModePick(kind.Key, false)))],
        [new TgButton(Texts.Get(lang, "bot.author_mode_gen"), BotLogic.Encode(new BotCallback.AuthorModePick(kind.Key, true)))],
    ];

    /// <summary>Qoralama ostidagi tugmalar: admin — chop etish; o'qituvchi — adminga yuborish; ikkalasi — o'chirish.</summary>
    public static IReadOnlyList<IReadOnlyList<TgButton>> DraftButtons(string lang, Guid id, bool isAdmin) =>
    [
        [
            isAdmin
                ? new TgButton(Texts.Get(lang, "bot.author_publish"), BotLogic.Encode(new BotCallback.TestCmd(TestAction.Publish, id)))
                : new TgButton(Texts.Get(lang, "bot.author_submit"), BotLogic.Encode(new BotCallback.TestCmd(TestAction.Submit, id))),
            new TgButton(Texts.Get(lang, "bot.author_delete"), BotLogic.Encode(new BotCallback.TestCmd(TestAction.Delete, id))),
        ],
    ];

    /// <summary>Admin uchun: tekshiruvdagi testni tasdiqlash yoki rad etish.</summary>
    public static IReadOnlyList<IReadOnlyList<TgButton>> ReviewButtons(string lang, Guid id) =>
    [
        [
            new TgButton(Texts.Get(lang, "bot.author_approve"), BotLogic.Encode(new BotCallback.TestCmd(TestAction.Publish, id))),
            new TgButton(Texts.Get(lang, "bot.author_reject"), BotLogic.Encode(new BotCallback.TestCmd(TestAction.Reject, id))),
        ],
    ];

    public static string Emoji(string module) => module switch
    {
        "speaking" => "🎙",
        "writing" => "✍️",
        "reading" => "📖",
        ShadowingRules.YouTube => "🎬",
        ShadowingRules.Character => "🐾",
        _ => "🎧",
    };

    /// <summary>Material yuborish ko'rsatmasi (tanlangan turga mos).</summary>
    public static string Instructions(string lang, AuthorKind kind, bool generate) =>
        !NeedsMode(kind)
            ? Texts.Get(lang, "bot.author_send_youtube", BotLogic.Html(kind.Label))
            : kind.IsShadowing
            ? Texts.Get(lang, generate ? "bot.author_send_topic" : "bot.author_send_src", BotLogic.Html(kind.Label)) + "\n" + Texts.Get(lang, "bot.author_character_note")
            : generate
            ? Texts.Get(lang, "bot.author_send_topic", BotLogic.Html(kind.Label))
            : Texts.Get(lang, "bot.author_send_src", BotLogic.Html(kind.Label)) + (kind.Module == "listening" ? "\n" + Texts.Get(lang, "bot.author_listening_note") : "");

    /// <summary>Qoralama xabari: tur, nom, qisqa mazmun va keyingi qadam.</summary>
    public static string DraftMessage(string lang, AuthorKind kind, string title, string summary, bool isAdmin) =>
        $"{Texts.Get(lang, "bot.author_ready", BotLogic.Html(kind.Label))}\n<b>{BotLogic.Html(title)}</b>\n\n<code>{BotLogic.Html(summary)}</code>\n\n{Texts.Get(lang, isAdmin ? "bot.author_ready_admin" : "bot.author_ready_teacher")}";

    /// <summary>Adminga: kim yubordi, tur, nom va qisqa mazmun (to'liq matn — faylda).</summary>
    public static string ReviewMessage(string lang, string authorEmail, AuthorKind kind, string title, string summary) =>
        $"{Texts.Get(lang, "bot.author_review", BotLogic.Html(authorEmail))}\n{Emoji(kind.Module)} {BotLogic.Html(kind.Label)}\n<b>{BotLogic.Html(title)}</b>\n\n<code>{BotLogic.Html(summary)}</code>";

    /// <summary>Fayl nomi: "ielts-reading-academic-3f2a9c1b.txt".</summary>
    public static string FileName(AuthorKind kind, Guid id) =>
        $"{kind.Exam}-{kind.Module}{(kind.Variant.Length > 0 ? "-" + kind.Variant : "")}-{id.ToString("N")[..8]}.txt";

    public const int MinSourceChars = 80;

    /// <summary>
    /// Kelgan xabarni tekshiradi: mavzu rejimida — qisqa matn; material
    /// rejimida — matn, PDF/.txt hujjat yoki rasm. Xato bo'lsa — matn kaliti.
    /// </summary>
    public static (AuthorInput? Input, string? Error, object[] Args) ReadInput(TgMessage msg, bool generate, AuthorKind? kind = null)
    {
        var text = msg.Text?.Trim();
        if (kind is not null && !NeedsMode(kind))
        {
            return ShadowingRules.ParseYouTube(text) is not null
                ? (new AuthorInput(text, null, null, null), null, [])
                : (null, "bot.author_bad_youtube", [(int)(ShadowingRules.MaxClip / 60)]);
        }
        if (generate)
        {
            return text is { Length: >= 3 } t && t.Length <= MockAuthoringRules.MaxTopicChars
                ? (new AuthorInput(null, null, null, t), null, [])
                : (null, "bot.author_topic_bad", [MockAuthoringRules.MaxTopicChars]);
        }

        const int mb = MockAuthoringRules.MaxFileBytes / (1024 * 1024);
        if (msg.Document is { } doc)
        {
            var mime = MockAuthoringRules.NormalizeMime(doc.MimeType, doc.FileName);
            if (mime is null) return (null, "bot.author_bad_file", []);
            if (doc.FileSize > MockAuthoringRules.MaxFileBytes) return (null, "bot.author_too_big", [mb]);
            return (new AuthorInput(null, doc.FileId, mime, null), null, []);
        }
        if (msg.Photo is { Length: > 0 } photos)
        {
            // Telegram bir nechta o'lcham yuboradi — eng kattasi matnni yaxshiroq o'qitadi.
            var best = photos.MaxBy(ph => (long)ph.Width * ph.Height)!;
            if (best.FileSize > MockAuthoringRules.MaxFileBytes) return (null, "bot.author_too_big", [mb]);
            return (new AuthorInput(null, best.FileId, "image/jpeg", null), null, []);
        }
        if (text is null) return (null, "bot.author_bad_file", []);
        if (text.Length < MinSourceChars) return (null, "bot.author_too_short", []);
        return (new AuthorInput(text, null, null, null), null, []);
    }

    public static IReadOnlyList<IReadOnlyList<TgButton>> CancelButtons(string lang) =>
        [[new TgButton(Texts.Get(lang, "bot.author_cancel_button"), BotLogic.Encode(new BotCallback.AuthorCancel()))]];

    public static string StatusLabel(string lang, string status) => Texts.Get(lang, "bot.status_" + status);

    /// <summary>/bank (admin): har tur bo'yicha chop etilgan va tekshiruvdagi testlar soni.</summary>
    public static string BankText(string lang, IReadOnlyDictionary<string, (int Published, int Pending)> counts)
    {
        var lines = AuthorKind.All.Select(k =>
        {
            var (pub, pen) = counts.TryGetValue(k.Key, out var c) ? c : (0, 0);
            return $"{Emoji(k.Module)} {BotLogic.Html(k.Label)}: <b>{pub}</b>" + (pen > 0 ? $" ({pen})" : "");
        });
        var pending = counts.Values.Sum(c => c.Pending);
        return $"{Texts.Get(lang, "bot.bank_title")}\n\n{string.Join("\n", lines)}\n\n<i>{Texts.Get(lang, "bot.bank_static")}</i>\n\n{Texts.Get(lang, "bot.bank_pending", pending)}";
    }

    /// <summary>/bank (o'qituvchi): oxirgi testlari va holati.</summary>
    public static string MineText(string lang, IEnumerable<(AuthorKind Kind, string Title, string Status)> tests)
    {
        var list = tests.ToList();
        if (list.Count == 0) return Texts.Get(lang, "bot.bank_empty");
        var lines = list.Select(t => $"{Emoji(t.Kind.Module)} {BotLogic.Html(t.Title)} — <i>{StatusLabel(lang, t.Status)}</i>");
        return $"{Texts.Get(lang, "bot.bank_mine")}\n\n{string.Join("\n", lines)}";
    }

    public static AuthorKind? KindOf(MockTest t) => AuthorKind.Parse($"{t.Exam}:{t.Module}:{t.Variant}");
}
