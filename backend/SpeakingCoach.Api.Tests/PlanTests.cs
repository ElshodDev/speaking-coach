using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class PlanTests
{
    private static readonly DateOnly Today = new(2026, 9, 27);

    private static PlanLogic.Input In(
        string? goal = "ielts", int? minutes = 20, DateOnly? exam = null, int total = 30, int due = 12, int reviewed = 0,
        string[]? practiced = null, string[]? mocked = null, Dictionary<string, DateTime>? last = null) =>
        new(goal, minutes, exam, Today, total, due, reviewed, 10,
            (practiced ?? []).ToHashSet(), (mocked ?? []).ToHashSet(), last ?? []);

    [Fact]
    public void Plan_has_review_skill_of_the_day_and_a_mock_for_exam_takers()
    {
        var p = PlanLogic.Build(In());
        Assert.Equal(["review", "practice", "mock"], p.Items.Select(x => x.Kind).ToArray());
        Assert.Equal(0, p.Items[0].Progress);
        Assert.Equal(10, p.Items[0].Target);
        Assert.StartsWith("practice/", p.Items[1].Route);
        Assert.Equal("mock:ielts:speaking", p.Items[2].Key);   // hali hech narsa topshirmagan — birinchi bo'lim
        Assert.Equal("mock/speaking", p.Items[2].Route);
        Assert.All(p.Items, x => Assert.False(x.Done));
    }

    [Fact]
    public void Skill_rotates_every_day()
    {
        var skills = Enumerable.Range(0, 4)
            .Select(d => PlanLogic.Build(In() with { Today = Today.AddDays(d) }).Items.First(x => x.Kind == "practice").Key)
            .ToList();
        Assert.Equal(4, skills.Distinct().Count());
    }

    [Fact]
    public void More_time_means_a_second_different_skill()
    {
        var p = PlanLogic.Build(In(minutes: 30));
        var practice = p.Items.Where(x => x.Kind == "practice").ToList();
        Assert.Equal(2, practice.Count);
        Assert.NotEqual(practice[0].Key, practice[1].Key);
        Assert.Single(PlanLogic.Build(In(minutes: 10)).Items.Where(x => x.Kind == "practice"));
    }

    [Fact]
    public void Items_are_done_by_what_happened_today()
    {
        var skill = PlanLogic.Skills[Today.DayNumber % 4];
        var p = PlanLogic.Build(In(reviewed: 10, practiced: [skill], mocked: ["reading"]));
        Assert.All(p.Items, x => Assert.True(x.Done));
        Assert.Equal("mock:ielts:reading", p.Items.Single(x => x.Kind == "mock").Key);   // bugun qilingani ko'rsatiladi
        // Takrorlanadigan karta qolmagan bo'lsa, 10 taga yetmasa ham bajarildi.
        Assert.True(PlanLogic.Build(In(due: 0, reviewed: 3)).Items[0].Done);
        Assert.False(PlanLogic.Build(In(due: 5, reviewed: 3)).Items[0].Done);
    }

    [Fact]
    public void No_review_item_without_cards_and_no_mock_for_general_english()
    {
        var p = PlanLogic.Build(In(goal: "general", total: 0, due: 0));
        Assert.Equal(["practice"], p.Items.Select(x => x.Kind).ToArray());
        Assert.Single(PlanLogic.Build(In(goal: null, total: 0, due: 0)).Items);
    }

    [Fact]
    public void Mock_is_weekly_unless_the_exam_is_close()
    {
        var recent = new Dictionary<string, DateTime> { ["listening"] = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc) };
        Assert.DoesNotContain(PlanLogic.Build(In(last: recent)).Items, x => x.Kind == "mock");
        // Imtihon 10 kundan keyin — har kuni bitta bo'lim.
        var soon = PlanLogic.Build(In(exam: Today.AddDays(10), last: recent));
        Assert.Equal(10, soon.DaysToExam);
        Assert.Equal("mock:ielts:speaking", soon.Items.Single(x => x.Kind == "mock").Key);
        // Bir haftadan eski — yana taklif.
        var old = new Dictionary<string, DateTime> { ["listening"] = new(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc) };
        Assert.Contains(PlanLogic.Build(In(last: old)).Items, x => x.Kind == "mock");
    }

    [Fact]
    public void Next_mock_module_is_the_one_never_taken_or_the_oldest()
    {
        var all = PlanLogic.Skills.ToDictionary(m => m, m => new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        all["writing"] = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("writing", PlanLogic.NextMockModule(all));
        Assert.Equal("writing", PlanLogic.NextMockModule(new Dictionary<string, DateTime> { ["speaking"] = DateTime.UtcNow }));
    }

    [Theory]
    [InlineData("cefr", "reading", "mock/cefr-reading")]
    [InlineData("ielts", "reading", "mock/reading-academic")]
    [InlineData("ielts", "writing", "mock/writing-academic")]
    [InlineData("ielts", "listening", "mock/listening")]
    public void Mock_routes(string goal, string module, string route) => Assert.Equal(route, PlanLogic.MockRoute(goal, module));

    [Fact]
    public void Past_exam_date_gives_no_countdown()
    {
        Assert.Null(PlanLogic.Build(In(exam: Today.AddDays(-1))).DaysToExam);
        Assert.Equal(0, PlanLogic.Build(In(exam: Today)).DaysToExam);
    }

    [Theory]
    [InlineData("ielts", "6.5", true)]
    [InlineData("ielts", "B2", false)]
    [InlineData("cefr", "B2", true)]
    [InlineData("cefr", "6.5", false)]
    [InlineData("general", "B2", false)]
    [InlineData("general", null, true)]
    [InlineData("ielts", "", true)]
    public void Target_must_match_the_exam(string goal, string? target, bool ok) => Assert.Equal(ok, PlanLogic.IsValidTarget(goal, target));

    [Fact]
    public void Onboarding_validation()
    {
        Assert.True(PlanLogic.IsValidGoal("cefr"));
        Assert.False(PlanLogic.IsValidGoal("toefl"));
        Assert.False(PlanLogic.IsValidGoal(null));
        Assert.True(PlanLogic.IsValidMinutes(null));
        Assert.True(PlanLogic.IsValidMinutes(45));
        Assert.False(PlanLogic.IsValidMinutes(25));
        Assert.True(PlanLogic.IsValidExamDate(Today, Today));
        Assert.False(PlanLogic.IsValidExamDate(Today.AddDays(-1), Today));
        Assert.False(PlanLogic.IsValidExamDate(Today.AddYears(3), Today));
        Assert.Equal(new DateOnly(2026, 12, 5), PlanLogic.ParseDate("2026-12-05"));
        Assert.Null(PlanLogic.ParseDate("05.12.2026"));
    }

    [Theory]
    [InlineData(ActivityType.Speaking, "speaking")]
    [InlineData(ActivityType.Listening, "listening")]
    [InlineData(ActivityType.MockExam, null)]
    public void Activity_to_skill(ActivityType type, string? skill) => Assert.Equal(skill, PlanLogic.SkillOf(type));
}
