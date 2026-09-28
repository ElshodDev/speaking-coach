using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class TalkTests
{
    [Fact]
    public void There_are_more_than_ten_distinct_scenarios_that_open_with_a_question()
    {
        Assert.True(Talk.Scenarios.Length > 10);
        Assert.Equal(Talk.Scenarios.Length, Talk.Scenarios.Select(s => s.Id).Distinct().Count());
        Assert.True(Talk.Scenarios.All(s => s.Opening.TrimEnd().EndsWith("?")));
        Assert.True(Talk.Scenarios.All(s => LearnerLevel.All.Contains(s.Level)));
        Assert.Null(Talk.Find("nope"));
    }

    [Fact]
    public void Turn_prompt_carries_role_history_level_and_feedback_language()
    {
        var s = Talk.Find("cafe")!;
        var history = new List<TalkLine> { new("ai", s.Opening), new("user", "I want one coffee") };
        var audio = Talk.BuildTurnPrompt(s, "B1", history, "ru", null);
        Assert.Contains("barista", audio);
        Assert.Contains("YOU: " + s.Opening, audio);
        Assert.Contains("LEARNER: I want one coffee", audio);
        Assert.Contains("attached audio", audio);
        Assert.Contains("Russian", audio);
        var typed = Talk.BuildTurnPrompt(s, "B1", history, "uz", "Can I have a latte?");
        Assert.Contains("typed this answer: \"Can I have a latte?\"", typed);
        Assert.DoesNotContain("attached audio", typed);
    }

    [Fact]
    public void Turn_result_is_cleaned_and_a_useless_tip_is_dropped()
    {
        var r = Talk.CleanTurn(new TalkTurnResult(" i want coffee ", " Sure! Hot or iced? ", new CorrectionItem("I want coffee", "i want coffee", "same")), null);
        Assert.Equal("i want coffee", r.Heard);
        Assert.Equal("Sure! Hot or iced?", r.Reply);
        Assert.Null(r.Tip);

        var typed = Talk.CleanTurn(new TalkTurnResult("something else", "Okay.", new CorrectionItem("I goed", "I went", "Past of go is went.")), "I goed there");
        Assert.Equal("I goed there", typed.Heard);
        Assert.Equal("I went", typed.Tip!.Corrected);

        Assert.Throws<InvalidOperationException>(() => Talk.CleanTurn(new TalkTurnResult("hi", "  ", null), null));
        var longReply = Talk.CleanTurn(new TalkTurnResult("x", new string('a', 900), null), null);
        Assert.True(longReply.Reply.Length <= Talk.MaxLineChars + 1);
    }

    [Fact]
    public void Summary_keeps_at_most_five_real_corrections_and_five_phrases()
    {
        var corrections = Enumerable.Range(1, 8).Select(i => new CorrectionItem($"bad {i}", $"good {i}", "why")).ToList();
        corrections.Add(new CorrectionItem("same", "Same", ""));
        var s = Talk.CleanSummary(new TalkSummary("Good talk.", "Clear ideas.", corrections, ["a", "b", "c", "d", "e", "f"], "Articles."));
        Assert.Equal(5, s.TopCorrections.Count);
        Assert.Equal(5, s.UsefulPhrases.Count);
        Assert.Throws<InvalidOperationException>(() => Talk.CleanSummary(new TalkSummary(" ", "", [], [], "")));
    }

    [Fact]
    public void User_turns_are_counted_from_the_server_side_history()
    {
        var state = new TalkState("cafe", "A2", Guid.NewGuid(), [new("ai", "Hi?"), new("user", "Hello"), new("ai", "What?"), new("user", "Tea")], []);
        Assert.Equal(2, Talk.UserTurns(state));
    }

    [Fact]
    public void Conversation_and_dictation_earn_xp_and_count_in_the_daily_plan()
    {
        var now = DateTime.UtcNow;
        Assert.Equal(ProgressCalculator.ConversationXp, ProgressCalculator.XpFor(new ActivityFact(ActivityType.Conversation, now)));
        Assert.Equal(ProgressCalculator.DictationXp, ProgressCalculator.XpFor(new ActivityFact(ActivityType.Dictation, now)));
        Assert.Equal("speaking", PlanLogic.SkillOf(ActivityType.Conversation));
        Assert.Equal("listening", PlanLogic.SkillOf(ActivityType.Dictation));
    }
}
