using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

public class CefrMockTests
{
    [Theory]
    // Rasmiy chegaralar (uzbmb.uz, Multilevel-bm.pdf): C1 65–75, B2 51–64, B1 38–50, B1 dan past 0–37.
    [InlineData(75, "C1")]
    [InlineData(65, "C1")]
    [InlineData(64, "B2")]
    [InlineData(51, "B2")]
    [InlineData(50, "B1")]
    [InlineData(38, "B1")]
    [InlineData(37, "below B1")]
    [InlineData(0, "below B1")]
    public void Official_level_cut_offs(int score, string level) => Assert.Equal(level, CefrScale.Level(score));

    [Theory]
    // Rasmiy jadval (uzbmb.uz, Multilevel-bm.pdf, 16.03.2023) — har bir qator chegarasi.
    [InlineData(0.0, 0)]
    [InlineData(0.1, 10)]
    [InlineData(0.5, 10)]
    [InlineData(0.6, 11)]
    [InlineData(10.0, 29)]
    [InlineData(14.0, 37)]
    [InlineData(14.1, 38)]
    [InlineData(20.5, 50)]
    [InlineData(20.6, 51)]
    [InlineData(26.6, 63)]
    [InlineData(27.0, 63)]
    [InlineData(27.1, 64)]
    [InlineData(28.0, 64)]
    [InlineData(28.1, 65)]
    [InlineData(29.5, 66)]
    [InlineData(30.3, 67)]
    [InlineData(30.6, 68)]
    [InlineData(31.2, 69)]
    [InlineData(31.9, 70)]
    [InlineData(32.4, 71)]
    [InlineData(33.0, 72)]
    [InlineData(33.9, 73)]
    [InlineData(34.5, 74)]
    [InlineData(36.0, 75)]
    [InlineData(40.0, 75)]
    public void Official_raw_to_75_table(double raw, int score) => Assert.Equal(score, CefrScale.Convert((decimal)raw));

    [Fact]
    public void Table_is_monotonic_and_integer_raw_bands_match_levels()
    {
        var prev = -1;
        for (var r = 0m; r <= 36m; r += 0.1m)
        {
            var s = CefrScale.Convert(r);
            Assert.True(s >= prev);
            prev = s;
        }
        Assert.Equal("below B1", CefrScale.Level(CefrScale.Convert(14)));
        Assert.Equal("B1", CefrScale.Level(CefrScale.Convert(15)));
        Assert.Equal("B1", CefrScale.Level(CefrScale.Convert(20)));
        Assert.Equal("B2", CefrScale.Level(CefrScale.Convert(21)));
        Assert.Equal("B2", CefrScale.Level(CefrScale.Convert(28)));
        Assert.Equal("C1", CefrScale.Level(CefrScale.Convert(29)));
    }

    [Fact]
    public void Writing_uses_official_12_plus_24_points()
    {
        Assert.Equal(12, CefrScale.WritingTask1Max);
        Assert.Equal(24, CefrScale.WritingTask2Max);
        Assert.Equal(75, CefrScale.WritingScore(12, 24));
        Assert.Equal(0, CefrScale.WritingScore(0, 0));
        Assert.Equal(55, CefrScale.WritingScore(8, 15));    // 23 → 55
        Assert.Equal(75, CefrScale.WritingScore(99, 99));   // chegaradan oshgani kesiladi
    }

    [Fact]
    public void Speaking_bank_matches_the_new_format()
    {
        Assert.True(CefrBank.Speaking.Length >= 6);
        foreach (var s in CefrBank.Speaking)
        {
            Assert.Equal(3, s.Part11.Length);
            Assert.Equal(2, s.Pictures.Length);
            Assert.Equal(3, s.Part12.Length);
            Assert.Equal(3, s.Part2Questions.Length);
            Assert.Equal(3, s.For.Length);
            Assert.Equal(3, s.Against.Length);
            Assert.Equal(8, s.Questions().Count);
            Assert.Equal("3", s.Questions()[7].Part);
        }
        Assert.Equal(60, CefrBank.Part2PrepSeconds);
        Assert.Equal(120, CefrBank.Part3SpeakSeconds);
    }

    [Fact]
    public void Writing_bank_states_the_word_ranges_in_each_task()
    {
        Assert.Equal((50, 70), CefrBank.Words11);
        Assert.Equal((120, 150), CefrBank.Words12);
        Assert.Equal((180, 200), CefrBank.Words2);
        foreach (var w in CefrBank.Writing)
        {
            Assert.Contains("50-70 words", w.Task11);
            Assert.Contains("120-150 words", w.Task12);
            Assert.Contains("180-200 words", w.Task2);
            Assert.Contains("friend", w.Task11);
        }
    }

    private static PartScore P(int s) => new(s, "because");

    [Fact]
    public void Speaking_result_uses_one_holistic_score_and_the_official_table()
    {
        var set = CefrBank.FindSpeaking("c1")!;
        var answers = Enumerable.Range(0, 8).Select(i => new SpokenAnswer(i, i == 7 ? null : [1, 2, 3], "audio/webm", 20)).ToList();
        var ai = new CefrSpeakingAi([new(0, "I like reading."), new(7, "should be ignored")], P(22), new("a", "b", "c", "d"), [], "good", ["a", "b", "c", "d"]);
        var r = GeminiCefrEvaluator.BuildSpeakingResult(set, answers, ai);
        Assert.Equal(53, r.Overall);   // 22 → 53
        Assert.Equal("B2", r.Level);
        Assert.Equal(22, r.Raw);
        Assert.Equal("I like reading.", r.Answers[0].Transcript);
        Assert.Equal("", r.Answers[7].Transcript);
        Assert.Equal("d", r.Parts[3].Comment);
        Assert.Equal(3, r.NextSteps.Count);
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildSpeakingResult(set, answers, ai with { Overall = P(37) }));
    }

    [Fact]
    public void Writing_result_uses_task1_12_and_task2_24()
    {
        var set = CefrBank.FindWriting("w1")!;
        CefrTaskAi T(int a, int b, int c, int d) => new(P(a), P(b), P(c), P(d));
        var ai = new CefrWritingAi(T(2, 2, 2, 2), T(4, 4, 4, 3), [], "ok", ["x"]);
        var r = GeminiCefrEvaluator.BuildWritingResult(set, "Hi Alex, the club is moving.", "Dear Secretary,", "", 1200, ai);
        Assert.Equal(23, r.Raw);
        Assert.Equal(55, r.Overall);
        Assert.Equal("B2", r.Level);
        Assert.Equal(12, r.Tasks[0].Max);
        Assert.Equal(24, r.Tasks[1].Max);
        Assert.Equal(6, r.Texts[0].Words);
        Assert.Equal(0, r.Texts[2].Words);
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildWritingResult(set, "", "", "", 0, ai with { Task1 = T(4, 0, 0, 0) }));  // task1 mezoni 0–3
        GeminiCefrEvaluator.BuildWritingResult(set, "", "", "", 0, ai with { Task2 = T(6, 6, 6, 6) });                                                   // task2 mezoni 0–6
    }

    [Fact]
    public void Prompts_describe_the_tasks_and_language()
    {
        var w = GeminiCefrEvaluator.BuildWritingPrompt(CefrBank.FindWriting("w2")!, "Hi", "", "Post", "ru");
        Assert.Contains("INFORMAL letter", w);
        Assert.Contains("TASK 1 (worth 12 points)", w);
        Assert.Contains("TASK 2 (worth 24 points)", w);
        Assert.Contains("TASK 1.2 (120-150 words)", w);
        Assert.Contains("(no answer)", w);
        Assert.Contains("Russian", w);
        var s = GeminiCefrEvaluator.BuildSpeakingPrompt(CefrBank.FindSpeaking("c2")!, "uz");
        Assert.Contains("0-36 scale", s);
        Assert.Contains("29-36 = C1", s);
        Assert.Contains("Online learning is better", s);
    }
}
