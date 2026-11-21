using System.Text.Json;

public class SpeakingFourCriteriaTests
{
    private const string Core = """
        "transcript": "I have many parks in my city.",
        "fluency": {"score": 62, "reasoning": "pauses"},
        "grammar": {"score": 55, "reasoning": "have/has"},
        "vocabulary": {"score": 60, "reasoning": "basic"},
        "topCorrections": [{"original": "I have many parks", "corrected": "There are many parks", "explanation": "use there are"}],
        "encouragement": "Good start", "nextFocus": "there is/are"
        """;

    private static string Json(string extra = "") => "{" + Core + (extra.Length > 0 ? "," + extra : "") + "}";

    // ---- Prompt ----------------------------------------------------------

    [Fact]
    public void Prompt_is_unchanged_when_the_flag_is_off_and_extended_when_on()
    {
        var off = GeminiSpeakingService.BuildPrompt("Hobbies", "B1");
        var on = GeminiSpeakingService.BuildPrompt("Hobbies", "B1", fourCriteria: true);
        Assert.Equal(off, GeminiSpeakingService.BuildPrompt("Hobbies", "B1", fourCriteria: false));
        Assert.DoesNotContain("\"bands\"", off);
        Assert.DoesNotContain("priorityFix", off);
        Assert.Contains("\"bands\"", on);
        Assert.Contains("\"priorityFix\"", on);
        Assert.Contains("\"pronunciation\"", on);
        Assert.Contains("NOT an official result", on);
        Assert.Contains("exactly ONE thing", on);
        // Eski qismlar joyida: 0-100 rubrika va asosiy JSON maydonlari.
        Assert.Contains("SCORING RUBRIC (0-100 for each)", on);
        Assert.Contains("\"topCorrections\"", on);
        // Shablon to'g'ri to'ldirilgan (qolib ketgan {0}/{1} yo'q) va JSON qavslari saqlangan.
        Assert.DoesNotContain("{0}", on);
        Assert.DoesNotContain("{1}", on);
        Assert.Contains("Hobbies", on);
        Assert.Contains("{", on);
    }

    // ---- Tolerant parser -------------------------------------------------

    [Fact]
    public void Full_extras_are_read_and_overall_is_the_rounded_average()
    {
        var x = SpeakingFeedbackParser.Parse(Json("""
            "pronunciation": {"score": 71.4, "reasoning": "clear vowels, stress on 'parks'"},
            "bands": {"fluency": 6, "lexical": 5.5, "grammar": 5, "pronunciation": 6.5},
            "priorityFix": {"issue": "Use there are", "original": "I have many parks", "corrected": "There are many parks", "tip": "Say 'there is/are' for what exists."}
            """));
        Assert.Equal(new ScoreWithReasoning(71, "clear vowels, stress on 'parks'"), x.Pronunciation);
        Assert.Equal(new SpeakingBands(6m, 5.5m, 5m, 6.5m), x.Bands);
        Assert.Equal(5.75m, (6m + 5.5m + 5m + 6.5m) / 4m);
        Assert.Equal(6.0m, x.OverallBand);   // 5.75 -> 6.0 (yarim band yaxlitlash)
        Assert.Equal("Use there are", x.Fix!.Issue);
    }

    [Theory]
    [InlineData("""{"bands": {"fluency": 6, "lexical": 6, "grammar": 6}}""")]                         // pronunciation yo'q
    [InlineData("""{"bands": {"fluency": 6, "lexical": 6, "grammar": 6, "pronunciation": 12}}""")]     // 0-9 dan tashqari
    [InlineData("""{"bands": {"fluency": "high", "lexical": 6, "grammar": 6, "pronunciation": 6}}""")] // son emas
    [InlineData("""{"bands": 7}""")]
    public void Invalid_bands_give_null_not_an_exception(string fragment)
    {
        var x = SpeakingFeedbackParser.Parse(Json(fragment.Trim('{', '}')));
        Assert.Null(x.Bands);
        Assert.Null(x.OverallBand);
    }

    [Fact]
    public void Bands_are_rounded_to_half_steps()
    {
        var x = SpeakingFeedbackParser.Parse(Json("""
            "bands": {"fluency": 6.2, "lexical": 5.8, "grammar": 5.74, "pronunciation": 6.26}
            """));
        Assert.Equal(new SpeakingBands(6m, 6m, 5.5m, 6.5m), x.Bands);
    }

    [Fact]
    public void Broken_pronunciation_and_fix_are_dropped_individually()
    {
        var x = SpeakingFeedbackParser.Parse(Json("""
            "pronunciation": {"score": 140, "reasoning": "x"},
            "priorityFix": {"issue": "", "original": "a", "corrected": "b", "tip": "t"},
            "bands": {"fluency": 6, "lexical": 6, "grammar": 6, "pronunciation": 6}
            """));
        Assert.Null(x.Pronunciation);
        Assert.Null(x.Fix);
        Assert.NotNull(x.Bands);   // sog'lom qism saqlanadi
    }

    [Fact]
    public void Garbage_input_never_throws()
    {
        Assert.Null(SpeakingFeedbackParser.Parse("not json").Bands);
        Assert.Null(SpeakingFeedbackParser.Parse("[]").Fix);
        Assert.Null(SpeakingFeedbackParser.Parse("{}").Pronunciation);
    }

    [Fact]
    public void Long_texts_are_capped()
    {
        var x = SpeakingFeedbackParser.Parse(Json($$"""
            "priorityFix": {"issue": "{{new string('i', 900)}}", "original": "o", "corrected": "c", "tip": "t"}
            """));
        Assert.Equal(SpeakingFeedbackParser.MaxTextChars, x.Fix!.Issue.Length);
    }

    // ---- Eski natijalar va qat'iy o'qish buzilmagan ------------------------

    [Fact]
    public void Core_result_still_parses_strictly_and_old_results_without_extras_work()
    {
        var r = GeminiJson.DeserializeStrict<SpeakingEvaluationResult>(Json());
        Assert.Equal(62, r.Fluency.Score);
        Assert.Null(r.Pronunciation);
        Assert.Null(r.Bands);
        Assert.Null(r.PriorityFix);
        Assert.Null(r.OverallBand);
        // Asosiy maydon yetishmasa — oldingidek qat'iy xato (yangi maydonlar buni yumshatmaydi).
        Assert.ThrowsAny<InvalidOperationException>(() => GeminiJson.DeserializeStrict<SpeakingEvaluationResult>("""{"transcript":"x"}"""));
    }

    [Fact]
    public void Result_round_trips_with_and_without_extras()
    {
        var core = GeminiJson.DeserializeStrict<SpeakingEvaluationResult>(Json());
        var x = SpeakingFeedbackParser.Parse(Json("""
            "bands": {"fluency": 6, "lexical": 6, "grammar": 5.5, "pronunciation": 6},
            "priorityFix": {"issue": "i", "original": "o", "corrected": "c", "tip": "t"}
            """));
        var full = core with { Bands = x.Bands, PriorityFix = x.Fix, OverallBand = x.OverallBand };
        var json = JsonSerializer.Serialize(full);
        Assert.Contains("\"bands\":{\"fluency\":6,", json);
        Assert.Contains("\"overallBand\":6", json);
        var back = JsonSerializer.Deserialize<SpeakingEvaluationResult>(json)!;
        Assert.Equal(full.Bands, back.Bands);
        Assert.Equal(full.PriorityFix, back.PriorityFix);
        // Eski (qo'shimchasiz) yozuv ham o'qiladi — tarix sahifasi buzilmaydi.
        Assert.NotNull(JsonSerializer.Deserialize<SpeakingEvaluationResult>(Json()));
    }

    // ---- Karta va flag -----------------------------------------------------

    [Fact]
    public void Priority_fix_becomes_a_review_card_once()
    {
        var top = new[] { new CorrectionItem("I have many parks", "There are many parks", "use there are") };
        var fix = new PriorityFix("Use there are", "I am agree", "I agree", "No 'am' before agree.");
        var cards = ReviewCardFactory.FromCorrections(ReviewCardFactory.WithPriorityFix(top, fix));
        Assert.Equal(2, cards.Count);
        Assert.Contains(cards, c => c.Front == "I am agree" && c.Back == "I agree" && c.Note.Contains("No 'am' before agree."));
        // Asl ibora ro'yxatda bor bo'lsa — takrorlanmaydi; fix yo'q bo'lsa — ro'yxat o'zgarmaydi.
        Assert.Single(ReviewCardFactory.WithPriorityFix(top, new PriorityFix("x", "i have many parks", "y", "z")));
        Assert.Single(ReviewCardFactory.WithPriorityFix(top, null));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("true", true)]
    [InlineData("garbage", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public void Flag_defaults_to_on_and_turns_off_with_false(string? value, bool expected)
    {
        Assert.Equal(expected, new FeatureFlags(new FakeConfig("Features:SpeakingFourCriteria", value)).SpeakingFourCriteria);
    }

    /// <summary>Test uchun eng kichik IConfiguration (bitta kalit).</summary>
    private sealed class FakeConfig(string key, string? value) : Microsoft.Extensions.Configuration.IConfiguration
    {
        public string? this[string k]
        {
            get => k == key ? value : null;
            set => throw new NotSupportedException();
        }
        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotSupportedException();
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string k) => throw new NotSupportedException();
    }
}
