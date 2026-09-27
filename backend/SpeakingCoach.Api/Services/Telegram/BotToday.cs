using System.Globalization;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>Bot tugmasi uchun: yoki botning o'zidagi amal (callback), yoki saytdagi sahifa (url).</summary>
public record TodayButton(string Label, BotCallback? Callback, string? Route);

/// <summary>
/// Botdagi "📋 Bugun", kechki eslatma qo'shimchasi va "yangi vazifa"
/// xabari — toza funksiyalar (matn va tugmalar), testlanadi.
/// </summary>
public static class BotToday
{
    /// <summary>Mahalliy vaqt: "30.09 18:00".</summary>
    public static string FormatDue(DateTime dueUtc, int tzOffsetMinutes) =>
        dueUtc.AddMinutes(-tzOffsetMinutes).ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);

    public static string ExamName(string? goal) => goal == "cefr" ? "CEFR" : "IELTS";

    private static string Module(string m) => m switch
    {
        "speaking" => "Speaking",
        "writing" => "Writing",
        "reading" => "Reading",
        "listening" => "Listening",
        _ => m,
    };

    /// <summary>O'qituvchi vazifasi turi: "review", "practice:writing", "mock:cefr:reading".</summary>
    public static string KindLabel(string lang, string kind, int? target) => kind.Split(':') switch
    {
        ["review"] => Texts.Get(lang, "bot.kind_review", target ?? 0),
        ["shadowing"] => Texts.Get(lang, "bot.kind_shadowing"),
        ["practice", var skill] => Texts.Get(lang, $"bot.kind_{skill}"),
        ["mock", var exam, var module] => Texts.Get(lang, "bot.kind_mock", ExamName(exam), Module(module)),
        _ => kind,
    };

    public static string PlanItemLabel(string lang, PlanLogic.Item item) => item.Kind switch
    {
        "review" => Texts.Get(lang, "bot.plan_review", item.Progress ?? 0, item.Target ?? 0),
        _ => KindLabel(lang, item.Key, null),
    };

    public static bool IsOverdue(PendingTask t, DateTime nowUtc) => t.DueAtUtc is DateTime d && d < nowUtc;

    /// <summary>"📋 Bugun" xabari: reja (✅/⬜️), imtihongacha kunlar, o'qituvchi vazifalari va tugmalar.</summary>
    public static (string Html, List<TodayButton> Buttons) TodayMessage(
        string lang, PlanLogic.Plan plan, string? targetScore, IReadOnlyList<PendingTask> tasks, DateTime nowUtc, int tzOffsetMinutes)
    {
        var lines = new List<string> { Texts.Get(lang, "bot.today_title") };
        if (plan.Goal is "ielts" or "cefr" && plan.DaysToExam is int days)
        {
            var countdown = days == 0
                ? Texts.Get(lang, "bot.exam_today", ExamName(plan.Goal))
                : Texts.Get(lang, "bot.exam_countdown", ExamName(plan.Goal), days);
            lines.Add(targetScore is null ? countdown : $"{countdown} · {Texts.Get(lang, "bot.target", targetScore)}");
        }
        if (plan.Goal is null) lines.Add($"<i>{Texts.Get(lang, "bot.today_no_goal")}</i>");

        var buttons = new List<TodayButton>();
        if (plan.Items.Count > 0)
        {
            lines.Add("");
            foreach (var item in plan.Items)
            {
                var label = PlanItemLabel(lang, item);
                lines.Add($"{(item.Done ? "✅" : "⬜️")} {BotLogic.Html(label)}");
                if (!item.Done)
                {
                    buttons.Add(item.Kind == "review"
                        ? new TodayButton(label, new BotCallback.StartReview(), null)
                        : new TodayButton(label, null, item.Route));
                }
            }
            var done = plan.Items.Count(i => i.Done);
            lines.Add(done == plan.Items.Count
                ? Texts.Get(lang, "bot.today_all_done")
                : $"<i>{Texts.Get(lang, "bot.today_progress", done, plan.Items.Count)}</i>");
        }

        if (tasks.Count > 0)
        {
            lines.Add("");
            lines.Add(Texts.Get(lang, "bot.today_tasks"));
            foreach (var t in tasks.Take(5))
            {
                var label = KindLabel(lang, t.Kind, t.Target);
                var due = t.DueAtUtc is DateTime d
                    ? IsOverdue(t, nowUtc) ? Texts.Get(lang, "bot.task_overdue", FormatDue(d, tzOffsetMinutes)) : Texts.Get(lang, "bot.task_due", FormatDue(d, tzOffsetMinutes))
                    : "";
                var progress = t.Status.Progress is int p && t.Status.Target is int tg ? $" · {p}/{tg}" : "";
                lines.Add($"• {BotLogic.Html(label)}{progress} — {BotLogic.Html(t.GroupName)}{due}");
            }
            if (tasks.Count > 5) lines.Add($"<i>{Texts.Get(lang, "bot.more_tasks", tasks.Count - 5)}</i>");
            buttons.Add(new TodayButton(Texts.Get(lang, "bot.tasks_button"), null, "tasks"));
        }
        return (string.Join("\n", lines), buttons);
    }

    /// <summary>Kechki eslatmaga qo'shimcha qatorlar: vazifalar va imtihongacha kunlar.</summary>
    public static List<string> ReminderExtra(string lang, IReadOnlyList<PendingTask> tasks, string? goal, int? daysToExam, DateTime nowUtc)
    {
        var lines = new List<string>();
        if (tasks.Count > 0)
        {
            var overdue = tasks.Count(t => IsOverdue(t, nowUtc));
            lines.Add(overdue > 0 ? Texts.Get(lang, "bot.reminder_tasks_overdue", tasks.Count, overdue) : Texts.Get(lang, "bot.reminder_tasks", tasks.Count));
        }
        if (goal is "ielts" or "cefr" && daysToExam is int d)
        {
            lines.Add(d == 0 ? Texts.Get(lang, "bot.exam_today", ExamName(goal)) : Texts.Get(lang, "bot.exam_countdown", ExamName(goal), d));
        }
        return lines;
    }

    /// <summary>O'qituvchi yangi vazifa berganda o'quvchiga boradigan xabar.</summary>
    public static string NewTaskMessage(string lang, string teacher, string group, string kind, int? target, string instructions, DateTime? dueUtc, int tzOffsetMinutes)
    {
        var lines = new List<string>
        {
            Texts.Get(lang, "bot.new_task", BotLogic.Html(teacher), BotLogic.Html(group)),
            $"<b>{BotLogic.Html(KindLabel(lang, kind, target))}</b>",
        };
        if (!string.IsNullOrWhiteSpace(instructions))
        {
            var text = instructions.Length > 300 ? instructions[..300] + "…" : instructions;
            lines.Add($"<i>{BotLogic.Html(text)}</i>");
        }
        if (dueUtc is DateTime d) lines.Add(Texts.Get(lang, "bot.new_task_due", FormatDue(d, tzOffsetMinutes)));
        return string.Join("\n", lines);
    }
}
