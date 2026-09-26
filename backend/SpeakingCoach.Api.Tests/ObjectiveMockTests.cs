using System.Text.Json;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

public class ObjectiveMockTests
{
    private static QuestionGroup Gap(int max, params ObjQuestion[] qs) => new("gap", "Write NO MORE THAN TWO WORDS.", max, qs.ToList());
    private static ObjQuestion Q(int n, string prompt, params string[] answers) => new(n, prompt, null, answers.ToList(), "because");
    private static ObjQuestion Mcq(int n, string answer) => new(n, "Why?", ["a", "b", "c", "d"], [answer], "because");

    [Theory]
    [InlineData("  Library ", "library")]
    [InlineData("the  Old   Bridge.", "the old bridge")]
    [InlineData("“£15”", "“£15”")]
    [InlineData("St. Mary’s", "st. mary's")]
    [InlineData("", "")]
    public void Normalize(string input, string expected) => Assert.Equal(expected, ObjectiveGrading.Normalize(input));

    [Fact]
    public void Gap_answers_accept_alternatives_and_enforce_the_word_limit()
    {
        var g = Gap(2, Q(1, "Meet at the ___", "main gate", "front gate"));
        var q = g.Questions[0];
        Assert.True(ObjectiveGrading.IsCorrect(g, q, "Main Gate"));
        Assert.True(ObjectiveGrading.IsCorrect(g, q, "front gate."));
        Assert.False(ObjectiveGrading.IsCorrect(g, q, "the main gate")); // 3 so'z > 2
        Assert.False(ObjectiveGrading.IsCorrect(g, q, ""));
        Assert.False(ObjectiveGrading.IsCorrect(g, q, "gate"));
    }

    [Fact]
    public void Mcq_and_tfng_compare_case_insensitively()
    {
        var mcq = new QuestionGroup("mcq", "Choose A-D", null, [Mcq(1, "B")]);
        Assert.True(ObjectiveGrading.IsCorrect(mcq, mcq.Questions[0], "b"));
        Assert.False(ObjectiveGrading.IsCorrect(mcq, mcq.Questions[0], "C"));
        var tf = new QuestionGroup("tfng", "TRUE/FALSE/NOT GIVEN", null, [Q(2, "Statement", "NOT GIVEN")]);
        Assert.True(ObjectiveGrading.IsCorrect(tf, tf.Questions[0], "not given"));
        Assert.False(ObjectiveGrading.IsCorrect(tf, tf.Questions[0], "FALSE"));
    }

    [Theory]
    // ielts.org "IELTS scoring in detail": average marks at bands 5/6/7/8.
    [InlineData(16, 5.0)]
    [InlineData(23, 6.0)]
    [InlineData(30, 7.0)]
    [InlineData(35, 8.0)]
    [InlineData(40, 9.0)]
    [InlineData(0, 0.0)]
    [InlineData(33, 7.5)]   // 7.6 → 7.5
    [InlineData(27, 6.5)]   // 6.57 → 6.5
    public void Listening_and_academic_reading_bands(int raw, double band) =>
        Assert.Equal((decimal)band, ObjectiveGrading.BandFor(raw, ObjectiveGrading.ListeningAnchors));

    [Theory]
    [InlineData(15, 4.0)]
    [InlineData(23, 5.0)]
    [InlineData(30, 6.0)]
    [InlineData(35, 7.0)]
    [InlineData(38, 8.0)]   // 7 + 2·3/5 = 8.2 → 8.0
    public void General_training_reading_bands(int raw, double band) =>
        Assert.Equal((decimal)band, ObjectiveGrading.BandFor(raw, ObjectiveGrading.GeneralReadingAnchors));

    [Fact]
    public void Band_scales_when_a_test_has_fewer_than_40_questions() =>
        Assert.Equal(ObjectiveGrading.BandFor(30, ObjectiveGrading.ListeningAnchors), ObjectiveGrading.BandFor(15, ObjectiveGrading.ListeningAnchors, 20));

    private const string Source = "Visitors meet at the main gate at 9.30. Tickets cost 15 dollars. The tour lasts two hours.";

    [Fact]
    public void Validator_accepts_a_well_formed_group()
    {
        var groups = new List<QuestionGroup>
        {
            Gap(2, Q(1, "Meet at the ___", "main gate"), Q(2, "Tickets cost ___ dollars", "15", "fifteen")),
            new("mcq", "Choose", null, [Mcq(3, "A")]),
        };
        ObjectiveValidator.ValidateGroups(groups, 1, 3, Source, allowYesNo: false);
    }

    [Fact]
    public void Validator_rejects_bad_items()
    {
        void Bad(List<QuestionGroup> g, int first = 1, int count = 1) =>
            Assert.Throws<InvalidOperationException>(() => ObjectiveValidator.ValidateGroups(g, first, count, Source, allowYesNo: false));

        Bad([Gap(2, Q(2, "Meet at the ___", "main gate"))]);                       // noto'g'ri raqam
        Bad([Gap(2, Q(1, "Meet at the ___", "south entrance"))]);                  // javob matnda yo'q
        Bad([Gap(1, Q(1, "Meet at the ___", "main gate"))]);                       // so'z chegarasidan uzun
        Bad([Gap(2, Q(1, "Meet at the gate", "main gate"))]);                      // bo'sh joy yo'q
        Bad([new("mcq", "Choose", null, [Mcq(1, "E")])]);                          // variantda yo'q harf
        Bad([new("tfng", "T/F/NG", null, [Q(1, "x", "MAYBE")])]);                  // noto'g'ri tfng
        Bad([new("ynng", "Y/N/NG", null, [Q(1, "x", "YES")])]);                    // listening'da ynng yo'q
        Bad([new("essay", "?", null, [Q(1, "x", "y")])]);                          // noma'lum tur
    }

    private static ReadingTest SampleReading() => new("academic",
    [
        new ReadingPassage("Tours", Source, [Gap(2, Q(1, "Meet at the ___", "main gate")), new("tfng", "T/F/NG", null, [Q(2, "The tour is short.", "FALSE")])]),
    ]);

    [Fact]
    public void Client_view_never_contains_answers_or_explanations()
    {
        var json = JsonSerializer.Serialize(ClientView.Reading(Guid.NewGuid(), SampleReading()), GeminiMockGenerator.Web);
        Assert.DoesNotContain("main gate\"", json.Replace("Meet at the ___", ""));
        Assert.DoesNotContain("\"answers\"", json);
        Assert.DoesNotContain("\"explanation\"", json);
        Assert.DoesNotContain("FALSE", json);
        Assert.Contains("Meet at the ___", json);
    }

    [Fact]
    public void Grading_a_stored_test_end_to_end()
    {
        var test = new MockTest
        {
            Id = Guid.NewGuid(), Module = "reading", Variant = "academic",
            Payload = JsonSerializer.Serialize(SampleReading(), GeminiMockGenerator.Web),
        };
        var r = MockEndpoints.GradeObjective("reading", test, new Dictionary<string, string> { ["1"] = "Main gate", ["2"] = "TRUE" }, 600);
        Assert.Equal(1, r.Score);
        Assert.Equal(2, r.Total);
        Assert.True(r.Questions[0].Correct);
        Assert.False(r.Questions[1].Correct);
        Assert.Equal("TRUE", r.Questions[1].Given);
        Assert.Equal(ObjectiveGrading.BandFor(1, ObjectiveGrading.AcademicReadingAnchors, 2), r.Overall);
    }

    [Fact]
    public void Full_mock_needs_all_four_modules_and_uses_official_rounding()
    {
        var bands = new Dictionary<string, decimal?> { ["listening"] = 6.5m, ["reading"] = 6.5m, ["writing"] = 5.0m, ["speaking"] = 7.0m };
        Assert.Equal(6.5m, FullMock.Overall(bands)); // 6.25 → 6.5 (ielts.org example rule)
        bands["speaking"] = 7.5m;                    // 6.375 → 6.5
        Assert.Equal(6.5m, FullMock.Overall(bands));
        bands.Remove("writing");
        Assert.Null(FullMock.Overall(bands));
    }

    [Fact]
    public void Session_ids_are_normalized_guids_only()
    {
        var g = Guid.NewGuid();
        Assert.Equal(g.ToString("N"), MockLimit.CleanSession(g.ToString()));
        Assert.Null(MockLimit.CleanSession("'; drop table"));
        Assert.Null(MockLimit.CleanSession(null));
    }

    [Fact]
    public void Only_speaking_and_writing_count_towards_the_daily_limit()
    {
        Assert.True(MockLimit.IsAiAssessed("speaking"));
        Assert.True(MockLimit.IsAiAssessed("writing"));
        Assert.False(MockLimit.IsAiAssessed("listening"));
        Assert.False(MockLimit.IsAiAssessed("reading"));
    }

    [Fact]
    public void Prompts_ask_for_the_official_structure()
    {
        var r = GeminiMockGenerator.ReadingPrompt("academic", 3, 27, 14, "sleep");
        Assert.Contains("numbered 27 to 40", r);
        Assert.Contains("750-900 words", r);
        var gt = GeminiMockGenerator.ReadingPrompt("general", 1, 1, 13, "a pool");
        Assert.Contains("General Training", gt);
        var l = GeminiMockGenerator.ListeningPrompt(4, 31, "bees");
        Assert.Contains("numbered 31 to 40", l);
        Assert.Contains("lecture", l);
    }

    [Fact]
    public void Generated_part_validation()
    {
        var script = new List<ScriptLine> { new("Anna", "female", string.Join(" ", Enumerable.Repeat("The meeting point is the main gate near the old tower.", 45))) };
        var groups = new List<QuestionGroup>
        {
            Gap(2, Enumerable.Range(1, 10).Select(n => Q(n, $"Item {n}: meet at the ___", "main gate")).ToArray()),
        };
        GeminiMockGenerator.ValidatePart(new ListeningPart(1, "A call.", script, groups), 1);
        Assert.Throws<InvalidOperationException>(() => GeminiMockGenerator.ValidatePart(new ListeningPart(2, "A call.", script, groups), 1));
        var badVoice = new List<ScriptLine> { script[0] with { Voice = "robot" } };
        Assert.Throws<InvalidOperationException>(() => GeminiMockGenerator.ValidatePart(new ListeningPart(1, "A call.", badVoice, groups), 1));
    }
}
