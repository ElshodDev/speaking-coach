using System.Text.Json;

public class ReadinessDiagnosticTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private static string Prompt(string title) => JsonSerializer.Serialize(new { title, passage = "text", questions = Array.Empty<object>() });
    private static string Resp(int score, int total) => JsonSerializer.Serialize(new { score, total, results = Array.Empty<object>() });

    // ---- O'qish/Tinglash → taxminiy band -----------------------------------

    [Theory]
    [InlineData("A2", 4, 4)]
    [InlineData("B1", 4, 4)]
    [InlineData("B2", 4, 4)]
    [InlineData("C1", 4, 4)]
    [InlineData("B1", 0, 4)]
    [InlineData("B2", 2, 4)]   // 2.5 + 4 * 2/4 = 4.5
    [InlineData("B1", 3, 4)]   // 2.5 + 3 * 3/4 = 4.75 -> yarim bandga yaxlitlanadi
    public void Comprehension_band_scales_linearly_to_the_level_ceiling(string level, int score, int total)
    {
        var band = ComprehensionBand.Estimate(level, score, total)!.Value;
        Assert.InRange(band, ComprehensionBand.Floor, ComprehensionBand.Ceiling(level)!.Value);
        Assert.Equal(0m, band * 2m % 1m);   // yarim band qadami
        if (level == "B1" && score == 3) Assert.Equal(5.0m, band);
        if (score == total) Assert.Equal(ComprehensionBand.Ceiling(level), band);
        if (score == 0) Assert.Equal(ComprehensionBand.Floor, band);
    }

    [Fact]
    public void Comprehension_band_rejects_nonsense()
    {
        Assert.Null(ComprehensionBand.Estimate("B1", 5, 4));
        Assert.Null(ComprehensionBand.Estimate("B1", -1, 4));
        Assert.Null(ComprehensionBand.Estimate("B1", 1, 0));
        Assert.Null(ComprehensionBand.Estimate("Z9", 2, 4));
        Assert.Null(ComprehensionBand.Estimate(null, 2, 4));
        // Yuqori daraja — yuqori band (bir xil ulush uchun).
        Assert.True(ComprehensionBand.Estimate("C1", 3, 4) > ComprehensionBand.Estimate("A2", 3, 4));
    }

    // ---- Dalil: faqat darajasi ma'lum (tayyor) mashqlardan ------------------

    [Fact]
    public void Built_in_exercise_gives_practice_evidence_with_its_level()
    {
        var item = PracticeLibrary.For(ActivityType.Reading).First(x => x.Level == "B2");
        var ev = ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, Prompt(item.Exercise.Title), Resp(4, 4), Now)!;
        Assert.Equal("reading", ev.Skill);
        Assert.Equal("practice", ev.Source);
        Assert.Equal(6.5m, ev.Value);

        var listening = PracticeLibrary.For(ActivityType.Listening).First(x => x.Level == "B1");
        var l = ReadinessCalculator.FromComprehension("ielts", ActivityType.Listening, Prompt(listening.Exercise.Title), Resp(2, 4), Now)!;
        Assert.Equal("listening", l.Skill);
        Assert.Equal(4.0m, l.Value);   // 2.5 + 3 * 2/4 = 4.0

        // CEFR shkalasiga o'tkaziladi.
        var cefr = ReadinessCalculator.FromComprehension("cefr", ActivityType.Reading, Prompt(item.Exercise.Title), Resp(4, 4), Now)!;
        Assert.Equal(ReadinessCalculator.BandToCefr(6.5m), cefr.Value);
    }

    [Fact]
    public void Unknown_or_broken_input_gives_no_evidence()
    {
        // AI yozgan mashq (kutubxonada yo'q) — daraja noma'lum, dalil yo'q.
        Assert.Null(ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, Prompt("Some AI generated title"), Resp(4, 4), Now));
        Assert.Null(ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, "oops", Resp(4, 4), Now));
        Assert.Null(ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, "{}", Resp(4, 4), Now));
        var title = PracticeLibrary.For(ActivityType.Reading)[0].Exercise.Title;
        Assert.Null(ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, Prompt(title), "{}", Now));
        Assert.Null(ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, Prompt(title), "garbage", Now));
        Assert.Null(ReadinessCalculator.FromComprehension("ielts", ActivityType.Speaking, Prompt(title), Resp(4, 4), Now));
    }

    // ---- Gapirish 4 mezonli bahosi ---------------------------------------

    [Fact]
    public void Speaking_overall_band_is_used_only_when_asked_to()
    {
        const string json = """{"fluency":{"score":50},"grammar":{"score":50},"vocabulary":{"score":50},"overallBand":6.5}""";
        var off = ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, json, Now)!;
        Assert.Equal(ReadinessCalculator.PracticeToBand(50), off.Value);   // eski hisob: 0–100 ballni bandga o'girish
        var on = ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, json, Now, useSpeakingBands: true)!;
        Assert.Equal(6.5m, on.Value);
        Assert.Equal("practice", on.Source);
        // overallBand bo'lmasa yoki yaroqsiz bo'lsa — eski hisobga qaytadi.
        Assert.Equal(off.Value, ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, json.Replace("\"overallBand\":6.5", "\"overallBand\":12"), Now, true)!.Value);
        Assert.Equal(off.Value, ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, json.Replace(",\"overallBand\":6.5", ""), Now, true)!.Value);
    }

    // ---- Birinchi kun: to'rt mashq → tayyorlik bahosi (past ishonch) --------

    [Fact]
    public void Four_short_exercises_give_a_low_confidence_overall_estimate()
    {
        var read = PracticeLibrary.For(ActivityType.Reading).First(x => x.Level == "B1");
        var listen = PracticeLibrary.For(ActivityType.Listening).First(x => x.Level == "B1");
        var evidence = new[]
        {
            ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, Prompt(read.Exercise.Title), Resp(3, 4), Now)!,
            ReadinessCalculator.FromComprehension("ielts", ActivityType.Listening, Prompt(listen.Exercise.Title), Resp(4, 4), Now)!,
            ReadinessCalculator.FromPractice("ielts", ActivityType.Writing, """{"taskAchievement":{"score":60},"coherenceCohesion":{"score":55},"grammar":{"score":50},"vocabulary":{"score":55}}""", Now)!,
            ReadinessCalculator.FromPractice("ielts", ActivityType.Speaking, """{"fluency":{"score":60},"grammar":{"score":55},"vocabulary":{"score":58},"overallBand":5.5}""", Now, true)!,
        };
        var report = ReadinessCalculator.Build("ielts", "6.5", null, DateOnly.FromDateTime(Now), evidence, Now);

        Assert.NotNull(report.Overall);
        Assert.All(report.Skills, s => Assert.NotNull(s.Estimate));
        Assert.All(report.Skills, s => Assert.Equal("low", s.Confidence));   // mock yo'q — hech qachon "yuqori"
        Assert.All(report.Skills, s => Assert.Equal(1m, s.Estimate!.Value - s.Low!.Value));   // keng oraliq (±1 band)
        Assert.True(report.OverallHigh - report.OverallLow >= 1m);
        Assert.Equal("behind", report.Status);   // 6.5 maqsad, ~5.5 baho
        Assert.False(report.DiagnosticEnabled);  // flagni servis qo'yadi; hisoblagich o'zi o'zgartirmaydi
    }

    [Fact]
    public void Without_exercises_for_a_skill_the_overall_stays_unknown_and_that_skill_is_the_focus()
    {
        var read = PracticeLibrary.For(ActivityType.Reading).First(x => x.Level == "B1");
        var one = new[] { ReadinessCalculator.FromComprehension("ielts", ActivityType.Reading, Prompt(read.Exercise.Title), Resp(4, 4), Now)! };
        var report = ReadinessCalculator.Build("ielts", "6.5", null, DateOnly.FromDateTime(Now), one, Now);
        Assert.Null(report.Overall);
        Assert.Equal("unknown", report.Status);
        Assert.Equal("missing", report.FocusReason);
        Assert.NotNull(report.Skills.Single(s => s.Skill == "reading").Estimate);
    }

    // ---- Flag --------------------------------------------------------------

    [Theory]
    [InlineData(null, true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    public void Day_one_flag_defaults_to_on(string? value, bool expected)
    {
        Assert.Equal(expected, new FeatureFlags(new FlagConfig("Features:DayOneDiagnostic", value)).DayOneDiagnostic);
    }

    private sealed class FlagConfig(string key, string? value) : Microsoft.Extensions.Configuration.IConfiguration
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
