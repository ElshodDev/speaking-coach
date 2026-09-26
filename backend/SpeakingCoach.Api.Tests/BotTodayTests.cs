using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

public class BotTodayTests
{
    private static readonly DateTime Now = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);
    private static readonly AssignmentStatus Open = new(false, false, null, null, null, null, null);

    private static PlanLogic.Plan Plan(string? goal = "ielts", int? days = 23) => new(goal, days,
    [
        new("review", "review", "review", false, 5, 3, 10),
        new("practice:writing", "practice", "practice/writing", true, 10),
        new("mock:ielts:listening", "mock", "mock/listening", false, 40),
    ]);

    private static PendingTask Task(string kind, DateTime? due, string group = "8-A <sinf>", int? target = null, AssignmentStatus? status = null) =>
        new(Guid.NewGuid(), group, kind, target, "", due, status ?? Open);

    [Fact]
    public void Every_assignment_kind_has_a_label_in_every_language()
    {
        string[] kinds = ["review", "practice:speaking", "practice:writing", "practice:reading", "practice:listening",
            "mock:ielts:listening", "mock:ielts:reading", "mock:ielts:writing", "mock:ielts:speaking",
            "mock:cefr:listening", "mock:cefr:reading", "mock:cefr:writing", "mock:cefr:speaking"];
        foreach (var lang in Texts.Langs)
        {
            foreach (var k in kinds)
            {
                Assert.True(GroupLogic.IsValidKind(k));
                var label = BotToday.KindLabel(lang, k, 20);
                Assert.DoesNotContain("bot.", label);
                Assert.NotEqual(k, label);
            }
        }
        Assert.Equal("🏁 CEFR Reading — mock imtihon", BotToday.KindLabel("uz", "mock:cefr:reading", null));
        Assert.Equal("🔁 20 ta kartani takrorlash", BotToday.KindLabel("uz", "review", 20));
    }

    [Theory]
    [InlineData("review", "review")]
    [InlineData("practice:listening", "practice/listening")]
    [InlineData("mock:cefr:reading", "mock/cefr-reading")]
    [InlineData("mock:ielts:reading", "mock/reading-academic")]
    [InlineData("mock:ielts:writing", "mock/writing-academic")]
    [InlineData("mock:ielts:speaking", "mock/speaking")]
    [InlineData("mock:toefl:reading", "tasks")]
    public void Routes_match_the_website(string kind, string route) => Assert.Equal(route, GroupLogic.RouteFor(kind));

    [Fact]
    public void Today_message_lists_the_plan_countdown_and_tasks()
    {
        var tasks = new[] { Task("mock:cefr:writing", Now.AddHours(-2)), Task("review", Now.AddDays(2), target: 20, status: Open with { Progress = 5, Target = 20 }) };
        var (html, buttons) = BotToday.TodayMessage("uz", Plan(), "6.5", tasks, Now, -300);
        Assert.Contains("📋 <b>Bugungi reja</b>", html);
        Assert.Contains("⏳ IELTS imtihonigacha 23 kun · maqsad: 6.5", html);
        Assert.Contains("⬜️ 🔁 Takrorlash — 3/10 ta karta", html);
        Assert.Contains("✅ ✍️ Yozish mashqi", html);
        Assert.Contains("1/3 bajarildi", html);
        Assert.Contains("⚠️ muddati oʻtdi (27.09 15:00)", html);   // 10:00 UTC → Toshkent 15:00
        Assert.Contains("· 5/20", html);
        Assert.Contains("8-A &lt;sinf&gt;", html);                // HTML xavfsiz
        // Tugmalar: takrorlash — botning o'zida, mock — saytda, vazifalar — saytda.
        Assert.Equal(3, buttons.Count);
        Assert.IsType<BotCallback.StartReview>(buttons[0].Callback);
        Assert.Equal("mock/listening", buttons[1].Route);
        Assert.Equal("tasks", buttons[2].Route);
    }

    [Fact]
    public void Today_message_without_goal_or_tasks()
    {
        var plan = new PlanLogic.Plan(null, null, [new("practice:speaking", "practice", "practice/speaking", true, 10)]);
        var (html, buttons) = BotToday.TodayMessage("en", plan, null, [], Now, 0);
        Assert.Contains("No goal set", html);
        Assert.Contains("Today's plan is done", html);
        Assert.DoesNotContain("exam", html);
        Assert.Empty(buttons);
    }

    [Fact]
    public void Exam_day_and_russian_texts()
    {
        var (html, _) = BotToday.TodayMessage("ru", Plan("cefr", 0), null, [], Now, 0);
        Assert.Contains("Экзамен CEFR — сегодня", html);
    }

    [Fact]
    public void Reminder_extra_mentions_tasks_overdue_and_exam()
    {
        var lines = BotToday.ReminderExtra("uz", [Task("review", Now.AddDays(-1)), Task("practice:speaking", null)], "cefr", 12, Now);
        Assert.Equal(["📚 2 ta vazifa kutmoqda, 1 tasining muddati oʻtdi.", "⏳ CEFR imtihonigacha 12 kun"], lines);
        Assert.Empty(BotToday.ReminderExtra("uz", [], "general", 12, Now));
        Assert.Equal(["📚 1 ta vazifa kutmoqda."], BotToday.ReminderExtra("uz", [Task("review", null)], null, null, Now));
    }

    [Fact]
    public void New_task_message_is_escaped_and_shows_local_due_time()
    {
        var html = BotToday.NewTaskMessage("uz", "Elshod <b>", "8-A", "mock:ielts:writing", null, "Task 2 ni yozing & tekshiring", new DateTime(2026, 9, 30, 13, 0, 0, DateTimeKind.Utc), -300);
        Assert.Contains("👩‍🏫 <b>Elshod &lt;b&gt;</b> (8-A) yangi vazifa berdi:", html);
        Assert.Contains("<b>🏁 IELTS Writing — mock imtihon</b>", html);
        Assert.Contains("Task 2 ni yozing &amp; tekshiring", html);
        Assert.Contains("⏰ Muddat: 30.09 18:00", html);
        var long_ = BotToday.NewTaskMessage("en", "T", "G", "review", 5, new string('x', 400), null, 0);
        Assert.Contains("…", long_);
        Assert.DoesNotContain("Due", long_);
    }

    [Fact]
    public void Today_menu_and_callback()
    {
        Assert.Equal(MenuAction.Today, BotLogic.ParseMenu("/today"));
        Assert.Equal(MenuAction.Today, BotLogic.ParseMenu("/today@SpeakingCoachUzBot"));
        foreach (var lang in Texts.Langs) Assert.Equal(MenuAction.Today, BotLogic.ParseMenu(BotLogic.MenuLabel(lang, MenuAction.Today)));
        Assert.IsType<BotCallback.ShowToday>(BotLogic.Decode(BotLogic.Encode(new BotCallback.ShowToday())));
        Assert.Contains(BotLogic.MenuLabel("uz", MenuAction.Today), BotLogic.MainMenu("uz")[0]);
    }
}
