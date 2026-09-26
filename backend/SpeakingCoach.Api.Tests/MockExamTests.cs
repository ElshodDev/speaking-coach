using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

public class MockExamTests
{
    [Theory]
    [InlineData(6.0, 6.0)]
    [InlineData(6.125, 6.0)]
    [InlineData(6.25, 6.5)]   // rasmiy: .25 → keyingi yarim band
    [InlineData(6.5, 6.5)]
    [InlineData(6.625, 6.5)]
    [InlineData(6.75, 7.0)]   // rasmiy: .75 → keyingi butun band
    [InlineData(9.5, 9.0)]
    [InlineData(-1.0, 0.0)]
    public void Round_half_band_follows_official_rule(double input, double expected) =>
        Assert.Equal((decimal)expected, IeltsBand.RoundHalfBand((decimal)input));

    [Fact]
    public void Speaking_overall_is_the_rounded_mean()
    {
        Assert.Equal(6.5m, IeltsBand.Speaking(6, 7, 6, 6));   // 6.25
        Assert.Equal(7.0m, IeltsBand.Speaking(7, 7, 7, 6));   // 6.75
        Assert.Equal(6.5m, IeltsBand.Speaking(6, 7, 7, 6));   // 6.5
    }

    [Fact]
    public void Writing_task2_counts_twice()
    {
        // Task 1 = 5.0, Task 2 = 7.0 → (5 + 14) / 3 = 6.33 → 6.5
        Assert.Equal(6.5m, IeltsBand.Writing(5m, 7m));
        // Task 1 = 7.0, Task 2 = 5.0 → (7 + 10) / 3 = 5.67 → 5.5
        Assert.Equal(5.5m, IeltsBand.Writing(7m, 5m));
        // Rounding happens once at the end: 6.25 and 6.25 → 6.25 → 6.5
        Assert.Equal(6.5m, IeltsBand.Writing(6.25m, 6.25m));
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("Hello world", 2)]
    [InlineData("It's a well-known fact — 42 people agree.", 7)]
    [InlineData("• first line\n- second, third", 4)]
    public void Word_count(string text, int expected) => Assert.Equal(expected, IeltsBand.CountWords(text));

    [Fact]
    public void Limit_allows_until_quota_then_reports_retry_time()
    {
        var now = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        var two = new[] { now.AddHours(-5), now.AddHours(-1) };
        Assert.True(MockLimit.Check(two, 3, now).Allowed);

        var three = new[] { now.AddHours(-20), now.AddHours(-5), now.AddHours(-1) };
        var (allowed, retry) = MockLimit.Check(three, 3, now);
        Assert.False(allowed);
        Assert.Equal(now.AddHours(4), retry); // eng eskisi 24 soatdan chiqqanda

        var old = new[] { now.AddHours(-30), now.AddHours(-25), now.AddHours(-24.5) };
        Assert.True(MockLimit.Check(old, 3, now).Allowed);
    }

    [Fact]
    public void Pick_fresh_prefers_unused_then_oldest()
    {
        var sets = new[] { "a", "b", "c" };
        var rng = new Random(1);
        for (var i = 0; i < 20; i++)
            Assert.Equal("c", IeltsBank.PickFresh(sets, s => s, ["a", "b"], rng));
        // Hammasi ishlangan: eng eskisi (ro'yxat oxirida) tanlanadi.
        Assert.Equal("a", IeltsBank.PickFresh(sets, s => s, ["c", "b", "a"], rng));
    }

    [Fact]
    public void Speaking_bank_matches_the_format()
    {
        Assert.True(IeltsBank.Speaking.Length >= 6);
        Assert.Equal(IeltsBank.Speaking.Length, IeltsBank.Speaking.Select(s => s.Id).Distinct().Count());
        foreach (var s in IeltsBank.Speaking)
        {
            Assert.Equal(4, s.Part1.Length);
            Assert.Equal(3, s.Part2.Points.Length);
            Assert.Equal(4, s.Part3.Length);
            var q = s.Questions();
            Assert.Equal(9, q.Count);
            Assert.Equal(2, q[4].Part);
        }
        // Official Part 2 timing (ielts.org): 1 minute preparation, up to 2 minutes speaking.
        Assert.Equal(60, IeltsBank.Part2PrepSeconds);
        Assert.Equal(120, IeltsBank.Part2SpeakSeconds);
    }

    [Fact]
    public void Writing_bank_has_both_variants_and_valid_charts()
    {
        Assert.Contains(IeltsBank.Writing, w => w.Variant == IeltsBank.Academic);
        Assert.Contains(IeltsBank.Writing, w => w.Variant == IeltsBank.General);
        foreach (var w in IeltsBank.Writing)
        {
            if (w.Variant == IeltsBank.Academic)
            {
                var c = w.Task1.Chart;
                Assert.NotNull(c);
                Assert.All(c!.Series, s => Assert.Equal(c.Categories.Length, s.Values.Length));
            }
            else
            {
                Assert.Null(w.Task1.Chart);
            }
        }
        Assert.Equal(150, IeltsBank.Task1MinWords);
        Assert.Equal(250, IeltsBank.Task2MinWords);
        Assert.Equal(60, IeltsBank.WritingMinutes);
    }

    [Fact]
    public void Writing_prompt_contains_chart_data_word_counts_and_language()
    {
        var set = IeltsBank.FindWriting("a1")!;
        var prompt = GeminiIeltsEvaluator.BuildWritingPrompt(set, "The chart shows data.", "", "ru");
        Assert.Contains("2005 | 42 | 18 | 55", prompt);
        Assert.Contains("(4 words)", prompt);
        Assert.Contains("(no answer)", prompt);
        Assert.Contains("Russian", prompt);
        Assert.Contains("Academic", prompt);
    }

    [Fact]
    public void Speaking_prompt_uses_feedback_language()
    {
        var set = IeltsBank.FindSpeaking("s1")!;
        Assert.Contains("Uzbek", GeminiIeltsEvaluator.BuildSpeakingPrompt(set, [], "uz"));
        Assert.Contains("simple English", GeminiIeltsEvaluator.BuildSpeakingPrompt(set, [], "en"));
    }

    private static BandWithReasoning B(int band) => new(band, "because");

    [Fact]
    public void Writing_result_is_computed_on_the_server()
    {
        var set = IeltsBank.FindWriting("g1")!;
        var ai = new WritingMockAi(
            new WritingTaskAi(B(6), B(6), B(5), B(5)),   // 5.5
            new WritingTaskAi(B(7), B(6), B(6), B(6)),   // 6.25
            [], "good", ["a", "b", "c", "d"]);
        var r = GeminiIeltsEvaluator.BuildWritingResult(set, "one two three", "four five", 1800, ai);
        Assert.Equal(5.5m, r.Task1.Band);
        Assert.Equal(6.5m, r.Task2.Band);
        Assert.Equal(6.0m, r.Overall);   // (5.5 + 12.5) / 3 = 6.0
        Assert.Equal(3, r.Task1.Words);
        Assert.Equal(3, r.NextSteps.Count);
        Assert.Equal("general", r.Variant);
    }

    [Fact]
    public void Bands_outside_0_9_or_without_reasoning_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => GeminiIeltsEvaluator.EnsureBands(("x", new BandWithReasoning(10, "r"))));
        Assert.Throws<InvalidOperationException>(() => GeminiIeltsEvaluator.EnsureBands(("x", new BandWithReasoning(5, " "))));
        GeminiIeltsEvaluator.EnsureBands(("x", new BandWithReasoning(0, "empty answer")));
    }

    [Fact]
    public void Strict_parsing_rejects_missing_criteria()
    {
        const string ok = """
            {"answers":[{"index":0,"transcript":"I live in Tashkent"}],
             "fluencyCoherence":{"band":6,"reasoning":"r"},"lexicalResource":{"band":6,"reasoning":"r"},
             "grammaticalRange":{"band":5,"reasoning":"r"},"pronunciation":{"band":6,"reasoning":"r"},
             "topCorrections":[],"strengths":"s","nextSteps":["n"]}
            """;
        var parsed = GeminiClient.DeserializeStrict<SpeakingMockAi>(ok);
        Assert.Equal(5, parsed.GrammaticalRange.Band);

        var missing = ok.Replace("\"pronunciation\":{\"band\":6,\"reasoning\":\"r\"},", "");
        Assert.Throws<InvalidOperationException>(() => GeminiClient.DeserializeStrict<SpeakingMockAi>(missing));
    }

    [Fact]
    public void Prompt_metadata_and_overall_are_read_safely()
    {
        var p = MockEndpoints.ParsePrompt("""{"exam":"ielts","module":"writing","setId":"a2","variant":"academic"}""");
        Assert.Equal("writing", p.Module);
        Assert.Equal("a2", p.SetId);
        Assert.Equal("", MockEndpoints.ParsePrompt("not json").Module);
        Assert.Equal(6.5m, MockEndpoints.ReadOverall("""{"overall":6.5}"""));
        Assert.Null(MockEndpoints.ReadOverall("{}"));
    }

    [Fact]
    public void Mock_exam_gives_xp_but_does_not_count_as_a_fifth_exercise_kind()
    {
        Assert.Equal(ProgressCalculator.MockExamXp, ProgressCalculator.XpFor(new ActivityFact(Data.ActivityType.MockExam, DateTime.UtcNow)));
        var three = new List<ActivityFact>
        {
            new(Data.ActivityType.Speaking, DateTime.UtcNow),
            new(Data.ActivityType.Writing, DateTime.UtcNow),
            new(Data.ActivityType.Reading, DateTime.UtcNow, 2),
            new(Data.ActivityType.MockExam, DateTime.UtcNow),
        };
        var badges = ProgressCalculator.Badges(three, 0, 0);
        Assert.False(badges.Single(b => b.Id == "all_rounder").Earned);
    }
}
