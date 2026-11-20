using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class ReadinessTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 20);
    private static ReadinessEvidence Mock(string skill, decimal v, int daysAgo = 1) => new(skill, "mock", v, Now.AddDays(-daysAgo));

    private static List<ReadinessEvidence> FourMocks(decimal l, decimal r, decimal w, decimal s) =>
        [Mock("listening", l), Mock("reading", r), Mock("writing", w), Mock("speaking", s)];

    [Fact]
    public void No_exam_goal_gives_no_estimate()
    {
        var r = ReadinessCalculator.Build("general", null, null, Today, FourMocks(6, 6, 6, 6), Now);
        Assert.Equal("no_goal", r.Status);
        Assert.Null(r.Exam);
        Assert.Empty(r.Skills);
    }

    [Fact]
    public void Ielts_overall_uses_official_rounding_and_compares_to_target()
    {
        // (6.5 + 7 + 5.5 + 6) / 4 = 6.25 → 6.5 (IELTS: .25 yuqoriga)
        var r = ReadinessCalculator.Build("ielts", "7.0", Today.AddDays(30), Today, FourMocks(6.5m, 7m, 5.5m, 6m), Now);
        Assert.Equal(6.5m, r.Overall);
        Assert.Equal(0.5m, r.Gap);
        Assert.Equal("close", r.Status);
        Assert.Equal(30, r.DaysToExam);
        Assert.Equal("writing", r.FocusSkill);   // eng past
        Assert.Equal("weakest", r.FocusReason);
        Assert.Equal(9m, r.Max);
    }

    [Theory]
    [InlineData("6.0", "on_track")]
    [InlineData("7.5", "behind")]
    [InlineData(null, "no_target")]
    public void Status_from_gap(string? target, string expected) =>
        Assert.Equal(expected, ReadinessCalculator.Build("ielts", target, null, Today, FourMocks(6, 6, 6, 6), Now).Status);

    [Fact]
    public void A_skill_without_data_blocks_the_overall_and_becomes_the_focus()
    {
        var ev = new List<ReadinessEvidence> { Mock("listening", 7), Mock("writing", 5), Mock("speaking", 6) };
        var r = ReadinessCalculator.Build("ielts", "6.5", null, Today, ev, Now);
        Assert.Null(r.Overall);
        Assert.Equal("unknown", r.Status);
        Assert.Equal(("reading", "missing"), (r.FocusSkill, r.FocusReason));
        Assert.Equal("none", r.Skills.Single(s => s.Skill == "reading").Confidence);
    }

    [Fact]
    public void Recent_results_weigh_more_and_old_ones_are_ignored()
    {
        var s = ReadinessCalculator.ForSkill("ielts", "reading",
            [Mock("reading", 5, daysAgo: 60), Mock("reading", 7, daysAgo: 1), Mock("reading", 3, daysAgo: 120)], Now);
        // 60 kunlik — 1/4 og'irlik; 120 kunlik — hisobga kirmaydi → (5·0.25 + 7·~1) / 1.25 ≈ 6.6 → 6.5
        Assert.Equal(6.5m, s.Estimate);
        Assert.Equal(2, s.Mocks);
    }

    [Fact]
    public void Confidence_grows_with_evidence_and_narrows_the_range()
    {
        var one = ReadinessCalculator.ForSkill("ielts", "speaking", [new("speaking", "practice", 6, Now)], Now);
        Assert.Equal(("low", 5m, 7m), (one.Confidence, one.Low!.Value, one.High!.Value));
        var mock = ReadinessCalculator.ForSkill("ielts", "speaking", [Mock("speaking", 6)], Now);
        Assert.Equal("medium", mock.Confidence);
        var two = ReadinessCalculator.ForSkill("ielts", "speaking", [Mock("speaking", 6), Mock("speaking", 6, 3)], Now);
        Assert.Equal("high", two.Confidence);
        Assert.Equal((5.5m, 6.5m), (two.Low!.Value, two.High!.Value));
    }

    [Fact]
    public void Without_a_mock_confidence_stays_low_or_medium()
    {
        var practice = Enumerable.Range(0, 10).Select(i => new ReadinessEvidence("speaking", "practice", 6, Now.AddDays(-i))).ToList();
        Assert.Equal("low", ReadinessCalculator.ForSkill("ielts", "speaking", practice, Now).Confidence);
        var withPart2 = practice.Append(new("speaking", "part2", 6, Now)).ToList();
        Assert.Equal("medium", ReadinessCalculator.ForSkill("ielts", "speaking", withPart2, Now).Confidence);
        var withMock = withPart2.Append(Mock("speaking", 6)).ToList();
        Assert.Equal("high", ReadinessCalculator.ForSkill("ielts", "speaking", withMock, Now).Confidence);
    }

    [Fact]
    public void Cefr_uses_0_75_scale_levels_and_level_thresholds_as_targets()
    {
        var r = ReadinessCalculator.Build("cefr", "B2", null, Today, FourMocks(55, 48, 50, 52), Now);
        Assert.Equal(51m, r.Overall);        // 205/4 = 51.25 → 51
        Assert.Equal("B2", r.OverallLevel);
        Assert.Equal(51m, r.TargetValue);
        Assert.Equal("on_track", r.Status);
        Assert.Equal(75m, r.Max);
        Assert.Equal(65m, ReadinessCalculator.TargetValue("cefr", "C1"));
        Assert.Null(ReadinessCalculator.TargetValue("cefr", "A2"));
        Assert.Null(ReadinessCalculator.TargetValue("ielts", "ten"));
    }

    [Theory]
    [InlineData(4.0, 38)]
    [InlineData(5.5, 51)]
    [InlineData(7.0, 65)]
    [InlineData(9.0, 75)]
    [InlineData(6.25, 58)]
    public void Band_to_cefr_mapping(double band, int score) =>
        Assert.Equal(score, (int)Math.Round(ReadinessCalculator.BandToCefr((decimal)band)));

    [Fact]
    public void Practice_results_become_evidence()
    {
        var sp = ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking,
            """{"fluency":{"score":70},"grammar":{"score":60},"vocabulary":{"score":80}}""", Now)!;
        Assert.Equal(("speaking", "practice"), (sp.Skill, sp.Source));
        Assert.Equal(6.9m, sp.Value); // 2 + 70·0.07
        var p2 = ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, """{"estimatedBand":6.5,"fluency":{"score":1}}""", Now)!;
        Assert.Equal(("part2", 6.5m), (p2.Source, p2.Value));
        var wr = ReadinessCalculator.FromPractice("cefr", ActivityType.Writing,
            """{"taskAchievement":{"score":50},"coherenceCohesion":{"score":50},"grammar":{"score":50},"vocabulary":{"score":50}}""", Now)!;
        Assert.Equal("writing", wr.Skill);
        Assert.InRange(wr.Value, 51, 52); // band 5.5 → CEFR 51
        Assert.Null(ReadinessCalculator.FromPractice("ielts", ActivityType.Reading, "{}", Now));
        Assert.Null(ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, """{"fluency":{"score":70}}""", Now));
        Assert.Null(ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, "oops", Now));
    }

    [Fact]
    public void Plan_puts_the_focus_skill_first_without_duplicates()
    {
        Assert.Equal(["writing", "speaking"], PlanLogic.PracticeSkills("writing", 0, 2)); // aylanish 0-kunda speaking'dan
        Assert.Equal(["speaking", "writing"], PlanLogic.PracticeSkills("speaking", 0, 2));
        Assert.Equal(["reading"], PlanLogic.PracticeSkills("reading", 5, 1));
        Assert.Equal(PlanLogic.PracticeSkills(null, 7, 2), [PlanLogic.Skills[7 % 4], PlanLogic.Skills[8 % 4]]);
    }

    [Fact]
    public void Plan_uses_focus_only_for_exam_goals_and_mocks_a_missing_section_first()
    {
        var empty = new HashSet<string>();
        var lastMock = new Dictionary<string, DateTime> { ["listening"] = Now.AddDays(-20), ["speaking"] = Now.AddDays(-30) };
        PlanLogic.Plan Make(string goal, string? reason) => PlanLogic.Build(new PlanLogic.Input(
            goal, 30, null, Today, 0, 0, 0, 20, empty, empty, lastMock, "reading", reason));

        var exam = Make("ielts", "missing");
        Assert.Equal("practice:reading", exam.Items.First(i => i.Kind == "practice").Key);
        Assert.Equal("mock:ielts:reading", exam.Items.Single(i => i.Kind == "mock").Key);
        Assert.Equal(("reading", "missing"), (exam.FocusSkill, exam.FocusReason));

        var general = Make("general", "weakest");
        Assert.Null(general.FocusSkill);
        Assert.DoesNotContain(general.Items, i => i.Kind == "mock");
    }
}
