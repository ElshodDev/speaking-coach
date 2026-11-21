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

    [Fact]
    public void Combine_weights_by_time_not_by_answer()
    {
        var a = FluencyMetrics.Analyze(string.Join(' ', Enumerable.Repeat("word", 60)), 60, 1, 1.5); // 60 so'z/daq
        var b = FluencyMetrics.Analyze("um uh " + string.Join(' ', Enumerable.Repeat("word", 30)), 10, 0, 0); // 180 so'z/daq
        var c = FluencyMetrics.Combine([a, b])!;
        Assert.Equal(90, c.Words);
        Assert.Equal(70, c.Seconds);
        Assert.Equal(77, c.Wpm);           // 90 / (70/60) — oddiy o'rtacha (120) emas
        Assert.Equal("slow", c.Pace);
        Assert.Equal(2, c.Fillers);
        Assert.Equal(1, c.LongPauses);
        Assert.Equal(1.5, c.PauseSeconds);
        Assert.Null(FluencyMetrics.Combine([]));
    }

    [Fact]
    public void Combine_hides_pauses_unless_every_part_was_measured()
    {
        var a = FluencyMetrics.Analyze("one two three four five six", 10, 2, 3);
        var b = FluencyMetrics.Analyze("one two three four five six", 10);
        Assert.Null(FluencyMetrics.Combine([a, b])!.LongPauses);
    }

    [Fact]
    public void Mock_answers_use_per_answer_pause_fields_and_skip_empty_answers()
    {
        var form = new Dictionary<string, string> { ["p0"] = "2", ["ps0"] = "3.5", ["p2"] = "1", ["ps2"] = "1.2" };
        var m = FluencyMetrics.ForAnswers(
            [(0, "I live in Tashkent, um, with my family", 20), (1, "", 0), (2, "I like reading books", 15)],
            k => form.GetValueOrDefault(k))!;
        Assert.Equal(35, m.Seconds);
        Assert.Equal(3, m.LongPauses);
        Assert.Equal(4.7, m.PauseSeconds);
        Assert.Equal(1, m.Fillers);
        Assert.Null(FluencyMetrics.ForAnswers([(0, "", 0)], _ => null));
    }

    [Fact]
    public void Mock_results_keep_camel_case_names_with_metrics()
    {
        var web = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        var node = FluencyMetrics.WithMetricsNode(new { Overall = 6.5m, TopCorrections = Array.Empty<string>() }, FluencyMetrics.Analyze("hello there my friend", 6), web);
        var json = System.Text.Json.JsonSerializer.Serialize<object>(node, web);
        Assert.Contains("\"overall\":6.5", json);
        Assert.Contains("\"metrics\":{\"words\":4", json);
        Assert.NotNull(FluencyMetrics.FromResponse(json));
    }

    [Fact]
    public void Talk_state_without_fluency_still_loads()
    {
        var old = """{"scenario":"cafe","level":"B1","userId":"00000000-0000-0000-0000-000000000001","lines":[],"tips":[]}""";
        var state = System.Text.Json.JsonSerializer.Deserialize<TalkState>(old, Talk.Json)!;
        Assert.Null(state.Fluency);
        var next = state with { Fluency = [FluencyMetrics.Analyze("um yes", 6)] };
        var back = System.Text.Json.JsonSerializer.Deserialize<TalkState>(System.Text.Json.JsonSerializer.Serialize(next, Talk.Json), Talk.Json)!;
        Assert.Single(back.Fluency!);
        Assert.Equal(1, back.Fluency![0].Fillers);
    }
}
