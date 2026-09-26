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

    [Fact]
    public void Speaking_raw_21_maps_to_75()
    {
        Assert.Equal(75, CefrScale.SpeakingScore([5, 5, 5, 6]));
        Assert.Equal(0, CefrScale.SpeakingScore([0, 0, 0, 0]));
        Assert.Equal(54, CefrScale.SpeakingScore([4, 4, 3, 4]));   // 15/21·75 = 53.57 → 54
        Assert.Equal(75, CefrScale.SpeakingScore([9, 9, 9, 9]));   // chegaradan oshgani kesiladi
    }

    [Fact]
    public void Writing_weights_sum_to_75_and_follow_task_size()
    {
        Assert.Equal(75m, CefrScale.WritingWeights.Sum());
        Assert.Equal(75, CefrScale.WritingScore([20, 20, 20]));
        Assert.Equal(0, CefrScale.WritingScore([0, 0, 0]));
        // 1.1: 10/20·12.5 = 6.25; 1.2: 15/20·25 = 18.75; 2: 16/20·37.5 = 30 → 55
        Assert.Equal(55, CefrScale.WritingScore([10, 15, 16]));
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
    public void Speaking_result_is_computed_on_the_server()
    {
        var set = CefrBank.FindSpeaking("c1")!;
        var answers = Enumerable.Range(0, 8).Select(i => new SpokenAnswer(i, i == 7 ? null : [1, 2, 3], "audio/webm", 20)).ToList();
        var ai = new CefrSpeakingAi([new(0, "I like reading."), new(7, "should be ignored")], P(4), P(4), P(3), P(4), [], "good", ["a", "b", "c", "d"]);
        var r = GeminiCefrEvaluator.BuildSpeakingResult(set, answers, ai);
        Assert.Equal(54, r.Overall);
        Assert.Equal("B2", r.Level);
        Assert.Equal("I like reading.", r.Answers[0].Transcript);
        Assert.Equal("", r.Answers[7].Transcript);   // audio yo'q — AI matni olinmaydi
        Assert.Equal(6, r.Parts[3].Max);
        Assert.Equal(3, r.NextSteps.Count);
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildSpeakingResult(set, answers, ai with { Part3 = P(7) }));
    }

    [Fact]
    public void Writing_result_is_computed_on_the_server()
    {
        var set = CefrBank.FindWriting("w1")!;
        CefrTaskAi T(int a, int b, int c, int d) => new(P(a), P(b), P(c), P(d));
        var ai = new CefrWritingAi(T(3, 2, 3, 2), T(4, 4, 4, 3), T(4, 4, 4, 4), [], "ok", ["x"]);
        var r = GeminiCefrEvaluator.BuildWritingResult(set, "Hi Alex, the club is moving.", "Dear Secretary,", "", 1200, ai);
        Assert.Equal(55, r.Overall);
        Assert.Equal("B2", r.Level);
        Assert.Equal(6, r.Tasks[0].Words);
        Assert.Equal(0, r.Tasks[2].Words);
        Assert.Equal(19, r.Tasks[1].Points);   // 15/20·25 = 18.75 → 19
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildWritingResult(set, "", "", "", 0, ai with { Task2 = T(6, 0, 0, 0) }));
    }

    [Fact]
    public void Prompts_describe_the_tasks_and_language()
    {
        var w = GeminiCefrEvaluator.BuildWritingPrompt(CefrBank.FindWriting("w2")!, "Hi", "", "Post", "ru");
        Assert.Contains("INFORMAL letter", w);
        Assert.Contains("TASK 1.2 (120-150 words)", w);
        Assert.Contains("(no answer)", w);
        Assert.Contains("Russian", w);
        var s = GeminiCefrEvaluator.BuildSpeakingPrompt(CefrBank.FindSpeaking("c2")!, "uz");
        Assert.Contains("part3 0-6", s);
        Assert.Contains("Online learning is better", s);
    }
}
