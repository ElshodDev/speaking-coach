using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class FluencyMetricsTests
{
    [Fact]
    public void Counts_fillers_and_excludes_them_from_words()
    {
        var m = FluencyMetrics.Analyze("Um, I think, uh, my city is, erm, very beautiful, you know.", 10);
        Assert.Equal(4, m.Fillers); // um, uh, erm, you know
        Assert.Equal(7, m.Words);   // i think my city is very beautiful
        Assert.Contains(m.TopFillers, f => f.Word == "you know");
        Assert.Contains(m.TopFillers, f => f.Word == "er"); // erm → er
    }

    [Fact]
    public void Words_per_minute_and_pace()
    {
        // 30 so'z 15 soniyada = 120 so'z/daqiqa — tabiiy.
        var text = string.Join(' ', Enumerable.Repeat("I really like my home town", 5));
        var m = FluencyMetrics.Analyze(text, 15);
        Assert.Equal(30, m.Words);
        Assert.Equal(120, m.Wpm);
        Assert.Equal("natural", m.Pace);
        Assert.Equal("slow", FluencyMetrics.PaceOf(60));
        Assert.Equal("fast", FluencyMetrics.PaceOf(200));
        Assert.Equal("unknown", FluencyMetrics.PaceOf(null));
    }

    [Fact]
    public void Rate_is_unknown_without_a_usable_duration()
    {
        var m = FluencyMetrics.Analyze("Hello my name is Aziz", null, 3, 4);
        Assert.Null(m.Wpm);
        Assert.Null(m.FillersPerMinute);
        Assert.Null(m.LongPauses);
        Assert.Equal("unknown", m.Pace);
        Assert.Null(FluencyMetrics.Analyze("Hello", 2).Wpm); // juda qisqa
    }

    [Fact]
    public void Detects_repetitions_and_self_corrections()
    {
        var m = FluencyMetrics.Analyze("I I think that, I think that we wen- went there, sorry, we go there", 20);
        Assert.Equal(2, m.Repetitions);      // "I I", "I think that I think that" (uch so'zlik)
        Assert.Equal(2, m.SelfCorrections);  // "wen-", "sorry"
    }

    [Theory]
    [InlineData("the the cat", 1)]
    [InlineData("I think I think so", 1)]
    [InlineData("my city my city my city", 2)]
    [InlineData("a b c d e", 0)]
    public void Repetition_groups(string text, int expected) =>
        Assert.Equal(expected, FluencyMetrics.CountRepetitions(FluencyMetrics.Tokenize(text)));

    [Fact]
    public void Handles_uzbek_and_russian_fillers()
    {
        var m = FluencyMetrics.Analyze("My job is, anaqa, teacher. Ну, I like it, xullas.", 8);
        Assert.Equal(3, m.Fillers); // anaqa, ну, xullas
        Assert.Equal(7, m.Words);   // my job is teacher i like it
    }

    [Fact]
    public void Empty_transcript_is_safe()
    {
        var m = FluencyMetrics.Analyze("", 30);
        Assert.Equal(0, m.Words);
        Assert.Equal(0, m.Wpm);
        Assert.Empty(m.TopFillers);
    }

    [Theory]
    [InlineData("42.5", "3", "6.1", 42.5, 3, 6.1)]
    [InlineData("abc", "3", "1", null, null, null)]   // vaqt noma'lum — pauzalar ham tashlanadi
    [InlineData("0", "1", "1", null, null, null)]
    [InlineData("5000", "1", "1", null, null, null)]  // juda uzun
    [InlineData("30", "-1", "40", 30.0, null, null)]  // manfiy pauza, vaqtdan uzun pauza
    public void Sanitizes_client_values(string sec, string pauses, string pauseSec, double? s, int? p, double? ps)
    {
        var (a, b, c) = FluencyMetrics.Sanitize(sec, pauses, pauseSec);
        Assert.Equal(s, a);
        Assert.Equal(p, b);
        Assert.Equal(ps, c);
    }

    [Fact]
    public void Metrics_round_trip_through_saved_response()
    {
        var eval = new SpeakingEvaluationResult("Um hello", new ScoreWithReasoning(70, "ok"), new ScoreWithReasoning(60, "ok"),
            new ScoreWithReasoning(65, "ok"), [], "Good", "Pace");
        var metrics = FluencyMetrics.Analyze(eval.Transcript, 12, 2, 3.5);
        var json = FluencyMetrics.WithMetrics(eval, metrics);

        Assert.Contains("\"transcript\":\"Um hello\"", json); // asl maydonlar joyida
        var back = FluencyMetrics.FromResponse(json);
        Assert.NotNull(back);
        Assert.Equal(metrics.Wpm, back!.Wpm);
        Assert.Equal(2, back.LongPauses);
        Assert.Null(FluencyMetrics.FromResponse("{\"transcript\":\"old row\"}"));
        Assert.Null(FluencyMetrics.FromResponse("not json"));
    }
}
