using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class AnswerUpgradeTests
{
    private const string Essay = "I think that phones is very bad for children. They play games all time and dont read books. Also it is bad for eyes.";

    [Theory]
    [InlineData("A2", "B1")]
    [InlineData("B1", "B2")]
    [InlineData("B2", "C1")]
    [InlineData("C1", "C1")]
    [InlineData(null, "B2")] // noma'lum → B1 → B2
    public void Default_target_is_one_step_up(string? level, string expected) =>
        Assert.Equal(expected, AnswerUpgradeRules.DefaultTarget(level));

    [Fact]
    public void Only_b1_b2_c1_are_targets()
    {
        Assert.Equal("B2", AnswerUpgradeRules.NormalizeTarget(" b2 "));
        Assert.Null(AnswerUpgradeRules.NormalizeTarget("A2"));
        Assert.Null(AnswerUpgradeRules.NormalizeTarget("C2"));
        Assert.Null(AnswerUpgradeRules.NormalizeTarget(null));
    }

    [Fact]
    public void Source_text_comes_from_the_right_field()
    {
        var w = AnswerUpgradeRules.SourceOf(ActivityType.Writing, """{"topic":"Phones","text":"My essay text"}""", "{}")!;
        Assert.Equal(("writing", "Phones", "My essay text"), (w.Kind, w.Topic, w.Text));

        var s = AnswerUpgradeRules.SourceOf(ActivityType.Speaking, """{"topic":"My city"}""", """{"transcript":" Um I live in Tashkent "}""")!;
        Assert.Equal(("speaking", "I live in Tashkent".Insert(0, "Um ")), (s.Kind, s.Text));

        var p2 = AnswerUpgradeRules.SourceOf(ActivityType.Speaking, """{"topic":"Describe…","part2":true,"cardId":"c1"}""", """{"transcript":"x y z"}""")!;
        Assert.Equal("part2", p2.Kind);

        Assert.Null(AnswerUpgradeRules.SourceOf(ActivityType.Reading, "{}", "{}"));
        Assert.Null(AnswerUpgradeRules.SourceOf(ActivityType.Speaking, "{}", """{"transcript":"  "}"""));
        Assert.Null(AnswerUpgradeRules.SourceOf(ActivityType.Writing, "not json", "{}"));
    }

    [Fact]
    public void Sanitize_drops_invented_quotes_unchanged_items_and_duplicates()
    {
        var ai = new UpgradeResult("B2", " Smartphones can be harmful to children. ",
        [
            new("phones is very bad", "smartphones can be harmful", "Grammar", "Agreement + better word"),
            new("They play games all time", "they spend hours gaming", "vocabulary", "Natural collocation"),
            new("They PLAY games  all time.", "dup", "vocabulary", "duplicate (case/space/punctuation)"),
            new("children love tablets", "invented", "vocabulary", "not in the learner text"),
            new("Also", "Also", "linking", "unchanged"),
            new("it is bad for eyes", "it can damage their eyesight", "weird-kind", "unknown kind → vocabulary"),
        ], " Summary ");
        var r = AnswerUpgradeRules.Sanitize(ai, Essay, "B2");
        Assert.Equal(3, r.Items.Count);
        Assert.Equal("grammar", r.Items[0].Kind);
        Assert.Equal("vocabulary", r.Items[2].Kind);
        Assert.DoesNotContain(r.Items, i => i.Upgraded == "invented");
        Assert.Equal("Smartphones can be harmful to children.", r.UpgradedText);
        Assert.Equal("B2", r.Target);
    }

    [Fact]
    public void Sanitize_caps_items()
    {
        var text = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"word{i}"));
        var items = Enumerable.Range(1, 20).Select(i => new UpgradeItem($"word{i}", $"better{i}", "vocabulary", "")).ToList();
        Assert.Equal(AnswerUpgradeRules.MaxItems, AnswerUpgradeRules.Sanitize(new UpgradeResult("C1", "x", items, ""), text, "C1").Items.Count);
    }

    [Fact]
    public void Result_is_cached_per_target_without_touching_other_fields()
    {
        const string response = """{"transcript":"t","fluency":{"score":60,"reasoning":"r"},"metrics":{"words":3}}""";
        var b2 = new UpgradeResult("B2", "better text", [new("a", "b", "vocabulary", "why")], "s");
        var json = AnswerUpgradeRules.WithCached(response, b2);
        json = AnswerUpgradeRules.WithCached(json, b2 with { Target = "C1", UpgradedText = "even better" });

        Assert.Contains("\"transcript\":\"t\"", json);
        Assert.Contains("\"metrics\":{\"words\":3}", json);
        Assert.Equal("better text", AnswerUpgradeRules.Cached(json, "B2")!.UpgradedText);
        Assert.Equal("even better", AnswerUpgradeRules.Cached(json, "C1")!.UpgradedText);
        Assert.Null(AnswerUpgradeRules.Cached(response, "B2"));
        Assert.Null(AnswerUpgradeRules.Cached("not json", "B2"));
        Assert.NotNull(FluencyMetrics.FromResponse("""{"metrics":{"words":3,"seconds":null,"wpm":null,"pace":"unknown","fillers":0,"fillersPerMinute":null,"topFillers":[],"repetitions":0,"selfCorrections":0,"longPauses":null,"pauseSeconds":null},"upgrades":{}}"""));
    }

    [Fact]
    public void Selected_items_become_review_cards()
    {
        var r = new UpgradeResult("B2", "x",
        [
            new("phones is very bad", "smartphones can be harmful", "grammar", "Agreement"),
            new("all time", "all the time", "grammar", "Article"),
        ], "");
        var cards = AnswerUpgradeRules.Cards(r, [1, 1, 5, -1]);
        var c = Assert.Single(cards);
        Assert.Equal(("all time", "all the time", "Article"), (c.Front, c.Back, c.Note));
        Assert.Equal(ReviewCardKind.Correction, c.Kind);
    }

    [Fact]
    public void Prompt_keeps_learner_ideas_and_style_by_kind()
    {
        var w = GeminiAnswerUpgrader.BuildPrompt(new UpgradeSource("writing", "Phones", Essay), "B1", "B2", "ru");
        Assert.Contains(Essay, w);
        Assert.Contains("keep the length within about 30%", w);
        Assert.Contains("B2 (upper-intermediate)", w);
        Assert.Contains("EXACT quote", w);
        var s = GeminiAnswerUpgrader.BuildPrompt(new UpgradeSource("speaking", "City", "um I live here"), "B1", "C1", "uz");
        Assert.Contains("SPOKEN English", s);
    }

    [Fact]
    public void Word_count() => Assert.Equal(4, AnswerUpgradeRules.WordCount("I don't like it."));
}
