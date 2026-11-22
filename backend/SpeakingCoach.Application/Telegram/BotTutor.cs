using System.Text;
using System.Text.RegularExpressions;
namespace SpeakingCoach.Application.Telegram;

/// <summary>Botda AI ustoz nimani kutayotgani: mavzu matni, o'z gaplari yoki savol.</summary>
public enum TutorAwait { Topic, Write, Ask }

/// <summary>Navbatdagi qadam: savol (bo'lim + indeks), yozma topshiriq taklifi yoki dars tugadi.</summary>
public record TutorNext(string Kind, string Section = "", int Index = 0);

/// <summary>
/// AI ustoz botda: holat yozuvi (TelegramAccount.BotState, ≤100 belgi), navbatdagi qadamni aniqlash va xabar matnlari —
/// toza funksiyalar (bazaga va tarmoqqa tegmaydi, testlanadi). Mantiq o'zi (TutorService) saytdagi bilan bir xil.
/// </summary>
public static partial class BotTutor
{
    public const string Prefix = "tu:";

    public static string State(TutorAwait kind, Guid? lessonId = null) => kind switch
    {
        TutorAwait.Topic => "tu:topic",
        TutorAwait.Write => $"tu:w:{lessonId:N}",
        _ => $"tu:q:{lessonId:N}",
    };

    public static (TutorAwait Kind, Guid? LessonId)? ParseState(string? state)
    {
        if (state is null || !state.StartsWith(Prefix, StringComparison.Ordinal)) return null;
        var p = state.Split(':');
        return p switch
        {
            ["tu", "topic"] => (TutorAwait.Topic, null),
            ["tu", "w", var id] when Guid.TryParseExact(id, "N", out var g) => (TutorAwait.Write, g),
            ["tu", "q", var id] when Guid.TryParseExact(id, "N", out var g) => (TutorAwait.Ask, g),
            _ => null,
        };
    }

    /// <summary>"tu:..." tugma ma'lumoti uchun javob variantining formati: "p0:2" (bo'lim harfi, savol, variant).</summary>
    [GeneratedRegex(@"^[pt]\d{1,2}:\d$")]
    private static partial Regex AnswerArg();

    public static bool IsAnswerArg(string arg) => AnswerArg().IsMatch(arg);

    public static (string Section, int Index, int Option) ParseAnswerArg(string arg)
    {
        var p = arg.Split(':');
        return (p[0][0] == 't' ? "test" : "practice", int.Parse(p[0][1..]), int.Parse(p[1]));
    }

    public static string AnswerArg(string section, int index, int option) => $"{(section == "test" ? 't' : 'p')}{index}:{option}";

    /// <summary>Server holatidan: avval mashqlar, so'ng (mini-test boshlanmagan bo'lsa) yozma taklifi, so'ng mini-test.</summary>
    public static TutorNext NextStep(TutorState s)
    {
        for (var i = 0; i < s.Practice.Count; i++)
            if (!s.Picked.ContainsKey(Tutor.Key("practice", i))) return new TutorNext("question", "practice", i);
        var testStarted = s.Test.Count > 0 && s.Picked.ContainsKey(Tutor.Key("test", 0));
        if (!testStarted && s.Writing is null) return new TutorNext("write");
        for (var i = 0; i < s.Test.Count; i++)
            if (!s.Picked.ContainsKey(Tutor.Key("test", i))) return new TutorNext("question", "test", i);
        return new TutorNext("done");
    }

    private static string Html(string? s) => BotLogic.Html(s);

    public static string ExplainText(string lang, TutorState s)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📖 <b>{Html(s.Topic)}</b> · {Html(s.Level)}");
        foreach (var e in s.Explain)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>{Html(e.Title)}</b>");
            sb.AppendLine(Html(e.Body));
        }
        return sb.ToString().TrimEnd();
    }

    public static string QuestionText(string lang, TutorState s, string section, int index)
    {
        var list = section == "test" ? s.Test : s.Practice;
        var head = Texts.Get(lang, section == "test" ? "tutor.bot_step_test" : "tutor.bot_step_practice", index + 1, list.Count);
        return $"<i>{Html(head)}</i>\n\n<b>{Html(list[index].Prompt)}</b>";
    }

    /// <summary>Variantlar — har biri alohida qatorda (uzun variantlar ham sig'sin).</summary>
    public static IReadOnlyList<IReadOnlyList<TgButton>> OptionButtons(Guid lessonId, string section, int index, TutorExercise q) =>
        [.. q.Options.Select((o, i) => (IReadOnlyList<TgButton>)[new TgButton(o, BotLogic.Encode(new BotCallback.Tutor(lessonId, "a", AnswerArg(section, index, i))))])];

    /// <summary>Javobdan keyingi xabar: savol + natija + izoh.</summary>
    public static string AnsweredText(string lang, TutorState s, string section, int index)
    {
        var list = section == "test" ? s.Test : s.Practice;
        var q = list[index];
        s.Picked.TryGetValue(Tutor.Key(section, index), out var picked);
        var ok = picked == q.AnswerIndex;
        var sb = new StringBuilder(QuestionText(lang, s, section, index));
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(ok ? Texts.Get(lang, "tutor.bot_correct") : Texts.Get(lang, "tutor.bot_wrong", Html(q.Options[q.AnswerIndex])));
        if (!ok && picked >= 0 && picked < q.Options.Count) sb.AppendLine($"<s>{Html(q.Options[picked])}</s>");
        if (!string.IsNullOrWhiteSpace(q.Explanation)) sb.AppendLine(Html(q.Explanation));
        return sb.ToString().TrimEnd();
    }

    public static string WritingText(string lang, TutorWriting w)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📝 {Html(w.Feedback)}");
        if (w.Corrections.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>{Texts.Get(lang, "tutor.bot_corrections")}</b>");
            foreach (var c in w.Corrections)
            {
                sb.AppendLine($"• <s>{Html(c.Original)}</s> → <b>{Html(c.Corrected)}</b>");
                if (!string.IsNullOrWhiteSpace(c.Explanation)) sb.AppendLine($"  <i>{Html(c.Explanation)}</i>");
            }
        }
        if (!string.IsNullOrWhiteSpace(w.Improved))
        {
            sb.AppendLine();
            sb.AppendLine($"<b>{Texts.Get(lang, "tutor.bot_improved")}:</b> {Html(w.Improved)}");
        }
        return sb.ToString().TrimEnd();
    }

    public static string ResultText(string lang, TutorFinished f)
    {
        var r = f.Result;
        var sb = new StringBuilder();
        sb.AppendLine(Texts.Get(lang, "tutor.bot_result", Html(f.State.Topic), r.PracticeCorrect, r.PracticeTotal, r.TestCorrect, r.TestTotal));
        sb.AppendLine();
        sb.AppendLine(Texts.Get(lang, $"tutor.bot_mastery_{(r.Mastery is "strong" or "ok" ? r.Mastery : "weak")}"));
        if (r.Mistakes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>{Texts.Get(lang, "tutor.bot_mistakes")}</b>");
            foreach (var m in r.Mistakes.Take(5)) sb.AppendLine($"• <s>{Html(m.Original)}</s> → <b>{Html(m.Corrected)}</b>");
        }
        if (f.NewCards > 0)
        {
            sb.AppendLine();
            sb.AppendLine(Texts.Get(lang, "tutor.bot_cards", f.NewCards));
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Tavsiya etilgan mavzular — har qatorda ikkita tugma.</summary>
    public static IReadOnlyList<IReadOnlyList<TgButton>> TopicButtons() =>
        [.. Tutor.SuggestedTopics
            .Select((t, i) => new TgButton(t, BotLogic.Encode(new BotCallback.Tutor(null, "t", i.ToString(System.Globalization.CultureInfo.InvariantCulture)))))
            .Chunk(2)
            .Select(row => (IReadOnlyList<TgButton>)row)];
}
