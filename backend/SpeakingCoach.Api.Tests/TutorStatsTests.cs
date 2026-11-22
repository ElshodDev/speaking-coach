using SpeakingCoach.Application.Telegram;

namespace SpeakingCoach.Api.Tests;

public class TutorStatsTests
{
    private static LessonRow Row(Guid? user, string topic, int testCorrect, string mastery, bool wrote = false) => new(
        user,
        $$"""{"topic":"{{topic}}","level":"B1"}""",
        $$"""{"result":{"practiceCorrect":5,"practiceTotal":6,"testCorrect":{{testCorrect}},"testTotal":4,"mastery":"{{mastery}}","wroteOwn":{{(wrote ? "true" : "false")}},"mistakes":[]},"writing":null}""");

    private static Dictionary<string, int> Events(int free = 0, int recommended = 0, int assigned = 0, int up = 0, int down = 0) => new()
    {
        ["lesson_start:free"] = free, ["lesson_start:recommended"] = recommended, ["lesson_start:assigned"] = assigned,
        ["lesson:up"] = up, ["lesson:down"] = down,
    };

    [Fact]
    public void Keys_are_consistent_with_the_rating_allow_list()
    {
        Assert.True(SpeakingCoach.Api.Endpoints.RatingRules.IsValid(TutorStats.RatingUp));
        Assert.True(SpeakingCoach.Api.Endpoints.RatingRules.IsValid(TutorStats.RatingDown));
        foreach (var s in TutorStats.Sources)
        {
            Assert.StartsWith(TutorStats.StartPrefix, TutorStats.StartKey(s));
            Assert.False(SpeakingCoach.Api.Endpoints.RatingRules.IsValid(TutorStats.StartKey(s)));   // ichki hodisa: tashqaridan yozib bo'lmaydi
        }
        Assert.Equal("lesson_start:free", TutorStats.StartKey("weird"));
    }

    [Fact]
    public void Aggregates_starts_completions_scores_mastery_topics_and_rating()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var rows = new[]
        {
            Row(a, "Articles (a/an/the)", 4, "strong", wrote: true),
            Row(a, "articles (a/an/the)", 2, "ok"),
            Row(b, "Modal verbs", 1, "weak"),
        };
        var r = TutorStats.Build(30, rows, Events(free: 2, recommended: 1, assigned: 3, up: 3, down: 1), lessonAssignments: 2);
        Assert.Equal(30, r.Days);
        Assert.Equal((6, 2, 1, 3), (r.Started, r.StartedFree, r.StartedRecommended, r.StartedAssigned));
        Assert.Equal(3, r.Completed);
        Assert.Equal(2, r.Learners);
        Assert.Equal(50, r.CompletionPercent);                  // 3 / 6
        Assert.Equal(58, r.AvgTestPercent);                     // (100 + 50 + 25) / 3
        Assert.Equal((1, 1, 1), (r.Strong, r.Ok, r.Weak));
        Assert.Equal(1, r.WroteOwn);
        Assert.Equal((3, 1), (r.RatedUp, r.RatedDown));
        Assert.Equal(75, r.HelpfulPercent);
        Assert.Equal(2, r.LessonAssignments);
        Assert.Equal(2, r.TopTopics.Count);
        Assert.Equal(2, r.TopTopics[0].Lessons);                // registr farqi bitta mavzu
        Assert.Equal("Articles (a/an/the)", r.TopTopics[0].Topic, ignoreCase: true);
    }

    [Fact]
    public void Empty_period_has_no_percentages_and_no_division_by_zero()
    {
        var r = TutorStats.Build(7, [], new Dictionary<string, int>(), 0);
        Assert.Equal((0, 0, 0), (r.Started, r.Completed, r.Learners));
        Assert.Null(r.CompletionPercent);
        Assert.Null(r.AvgTestPercent);
        Assert.Null(r.HelpfulPercent);
        Assert.Empty(r.TopTopics);
    }

    [Fact]
    public void Completion_percent_never_exceeds_100()
    {
        // Eski darslar (hodisa yozuvi bo'lmagan davrdan) tugatilishi mumkin.
        var rows = Enumerable.Range(0, 5).Select(_ => Row(Guid.NewGuid(), "X topic", 4, "strong")).ToList();
        Assert.Equal(100, TutorStats.Build(30, rows, Events(free: 2), 0).CompletionPercent);
    }

    [Fact]
    public void Unknown_keys_negative_counts_and_broken_rows_are_ignored()
    {
        var events = new Dictionary<string, int> { ["lesson_start:free"] = 2, ["source:instagram"] = 50, ["lesson:up"] = -4, ["lesson_start:other"] = 9 };
        var rows = new[] { new LessonRow(null, "not json", "not json"), new LessonRow(Guid.NewGuid(), null, null), Row(Guid.NewGuid(), "Modal verbs", 3, "strong") };
        var r = TutorStats.Build(30, rows, events, 0);
        Assert.Equal(2, r.Started);
        Assert.Equal(1, r.Completed);
        Assert.Equal(0, r.RatedUp);
        Assert.Single(r.TopTopics);
        Assert.Equal(75, r.AvgTestPercent);
    }

    [Fact]
    public void Top_topics_are_limited_and_sorted_by_popularity()
    {
        var rows = Enumerable.Range(1, 12).SelectMany(i => Enumerable.Range(0, i).Select(_ => Row(Guid.NewGuid(), $"Topic {i:00}", 3, "ok"))).ToList();
        var r = TutorStats.Build(30, rows, Events(), 0);
        Assert.Equal(TutorStats.MaxTopics, r.TopTopics.Count);
        Assert.Equal("Topic 12", r.TopTopics[0].Topic);
        Assert.Equal(12, r.TopTopics[0].Lessons);
        Assert.True(r.TopTopics.Zip(r.TopTopics.Skip(1)).All(p => p.First.Lessons >= p.Second.Lessons));
    }

    // ---- Bot ----

    [Fact]
    public void Bot_rating_callbacks_round_trip_and_reject_garbage()
    {
        foreach (var v in new[] { "u", "d" })
        {
            var cb = new BotCallback.Tutor(null, "r", v);
            Assert.Equal(cb, BotLogic.Decode(BotLogic.Encode(cb)));
        }
        Assert.Null(BotLogic.Decode("tu:r:x"));
        Assert.Null(BotLogic.Decode("tu:r:"));
        Assert.Null(BotLogic.Decode("tu:r:up"));
    }

    [Fact]
    public void Bot_recommended_and_plain_topic_arguments()
    {
        Assert.Equal((2, true), BotTutor.ParseTopicArg(BotTutor.RecommendedArg(2)));
        Assert.Equal((2, false), BotTutor.ParseTopicArg("2"));
        Assert.Null(BotTutor.ParseTopicArg("r"));
        Assert.Null(BotTutor.ParseTopicArg("r99"));
        Assert.Null(BotTutor.ParseTopicArg("99"));
        Assert.Null(BotTutor.ParseTopicArg("x2"));
        Assert.Null(BotTutor.ParseTopicArg("-1"));
        for (var i = 0; i < Tutor.AllTopics.Length; i++)
        {
            Assert.NotNull(BotLogic.Decode($"tu:t:{i}"));
            Assert.NotNull(BotLogic.Decode($"tu:t:r{i}"));
        }
        Assert.Null(BotLogic.Decode($"tu:t:r{Tutor.AllTopics.Length}"));
    }

    [Fact]
    public void Bot_result_buttons_have_a_rating_row_only_until_rated()
    {
        var with = BotTutor.ResultButtons("uz", true);
        var without = BotTutor.ResultButtons("uz", false);
        Assert.Equal(2, with.Count);
        Assert.Single(without);
        Assert.Equal(new BotCallback.Tutor(null, "r", "u"), BotLogic.Decode(with[1][0].CallbackData));
        Assert.Equal(new BotCallback.StartReview(), BotLogic.Decode(without[0][0].CallbackData));
        foreach (var key in new[] { "tutor.bot_rate_up", "tutor.bot_rate_down", "tutor.bot_thanks" })
            foreach (var lang in Texts.Langs) Assert.False(string.IsNullOrWhiteSpace(Texts.Get(lang, key)));
    }
}
