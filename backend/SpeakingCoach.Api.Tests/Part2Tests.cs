using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

public class Part2Tests
{
    private static readonly Part2Card Card = new("c1", "Describe a person who taught you something important.",
        ["who this person is", "how you know them", "what they taught you"], "and explain why this lesson was important to you.");

    [Fact]
    public void Bank_has_extra_cards_plus_every_mock_cue_card_with_unique_ids()
    {
        var all = Part2Rules.All(IeltsBank.Speaking);
        Assert.Equal(Part2Rules.Extra.Length + IeltsBank.Speaking.Length, all.Count);
        Assert.Equal(all.Count, all.Select(c => c.Id).Distinct().Count());
        Assert.True(all.Count >= 30);
        Assert.All(all, c =>
        {
            Assert.StartsWith("Describe", c.Topic);
            Assert.InRange(c.Points.Length, 3, 4);
            Assert.StartsWith("and explain", c.Explain);
        });
    }

    [Fact]
    public void Coverage_follows_card_order_and_fills_missing_points()
    {
        var ai = new List<CoverageItem>
        {
            new("What they taught you", "Covered", "she taught me to be patient"),
            new("who this person is.", "partly", "my teacher"),
        };
        var c = Part2Rules.NormalizeCoverage(Card, ai);
        Assert.Equal(4, c.Count); // 3 band + "and explain"
        Assert.Equal(["who this person is", "how you know them", "what they taught you", Card.Explain], c.Select(x => x.Point));
        Assert.Equal("partly", c[0].Status);
        Assert.Equal("covered", c[2].Status);      // katta harf, tartib boshqa — baribir topildi
        Assert.Equal("missed", c[3].Status);
        Assert.Equal("", c[3].Evidence);
    }

    [Fact]
    public void Coverage_rejects_unknown_statuses_and_null()
    {
        var c = Part2Rules.NormalizeCoverage(Card, [new("who this person is", "excellent", "x")]);
        Assert.Equal("missed", c[0].Status);
        Assert.All(Part2Rules.NormalizeCoverage(Card, null), x => Assert.Equal("missed", x.Status));
    }

    [Theory]
    [InlineData(6.3, 6.5)]
    [InlineData(6.2, 6.0)]
    [InlineData(11, 9)]
    [InlineData(-1, 0)]
    public void Band_is_clamped_to_half_steps(double given, double expected) =>
        Assert.Equal((decimal)expected, Part2Rules.ClampBand((decimal)given));

    [Fact]
    public void Coverage_percent_counts_partly_as_half()
    {
        var items = new List<CoverageItem> { new("a", "covered", ""), new("b", "partly", ""), new("c", "missed", ""), new("d", "covered", "") };
        Assert.Equal(63, Part2Rules.CoveragePercent(items));
        Assert.Equal(0, Part2Rules.CoveragePercent([]));
    }

    [Fact]
    public void Prompt_contains_the_card_and_feedback_language()
    {
        var p = GeminiPart2Coach.BuildPrompt(Card, "B1", "uz");
        Assert.Contains(Card.Topic, p);
        Assert.Contains("- how you know them", p);
        Assert.Contains(Card.Explain, p);
        Assert.Contains("\"estimatedBand\"", p);
        Assert.Contains("\"coverage\"", p);
        Assert.Contains(GeminiIeltsEvaluator.FeedbackLanguage("uz"), p);
    }

    [Fact]
    public void Card_id_is_read_only_from_part2_prompts()
    {
        Assert.Equal("c3", Part2Endpoints.CardIdOf("""{"topic":"t","part2":true,"cardId":"c3"}"""));
        Assert.Null(Part2Endpoints.CardIdOf("""{"topic":"my city"}"""));
        Assert.Null(Part2Endpoints.CardIdOf("not json"));
        Assert.Null(Part2Endpoints.CardIdOf(null));
    }
}
