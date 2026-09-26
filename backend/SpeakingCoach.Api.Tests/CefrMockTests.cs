using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

/// <summary>CEFR (Multilevel) — Bilim va malakalarni baholash agentligining rasmiy hujjatlari bo'yicha.</summary>
public class CefrMockTests
{
    [Theory]
    // "Baholash mezonlari" (16.03.2023): C1 65–75, B2 51–64, B1 38–50, B1 dan past 0–37.
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
    // Rasmiy jadval (0–36 → 0–75) — har bir o'zgarish nuqtasi.
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
    public void Table_is_monotonic()
    {
        var prev = -1;
        for (var r = 0m; r <= 36m; r += 0.1m)
        {
            var s = CefrScale.Convert(r);
            Assert.True(s >= prev);
            prev = s;
        }
    }

    [Fact]
    public void Writing_uses_official_12_plus_24_points_and_the_table()
    {
        Assert.Equal(12, CefrScale.WritingTask1Max);
        Assert.Equal(24, CefrScale.WritingTask2Max);
        Assert.Equal(75, CefrScale.WritingScore(12, 24));
        Assert.Equal(0, CefrScale.WritingScore(0, 0));
        Assert.Equal(55, CefrScale.WritingScore(8, 15));    // 23 → 55
        Assert.Equal(75, CefrScale.WritingScore(99, 99));
    }

    [Fact]
    public void Speaking_uses_the_official_rating_scale_5_5_5_6()
    {
        Assert.Equal(new[] { 5, 5, 5, 6 }, CefrScale.SpeakingMax);
        Assert.Equal(21, CefrScale.SpeakingRawMax);
        Assert.Equal(75, CefrScale.SpeakingScore([5, 5, 5, 6]));
        Assert.Equal(0, CefrScale.SpeakingScore([0, 0, 0, 0]));
        // Rasmiy tavsiflar bilan izchil: "Higher B2" (4 va 4) → B2, "Lower B1" (1 va 1) → B1 dan past emas.
        Assert.Equal("B2", CefrScale.Level(CefrScale.SpeakingScore([5, 5, 4, 4])));
        Assert.Equal("C1", CefrScale.Level(CefrScale.SpeakingScore([5, 5, 5, 5])));
        Assert.Equal("B1", CefrScale.Level(CefrScale.SpeakingScore([5, 4, 1, 1])));
        Assert.Equal(75, CefrScale.SpeakingScore([9, 9, 9, 9]));
    }

    [Fact]
    public void Speaking_bank_matches_the_official_new_format()
    {
        Assert.True(CefrBank.Speaking.Length >= 6);
        foreach (var s in CefrBank.Speaking)
        {
            Assert.Equal(3, s.Part11.Length);          // 1.1 — 3 savol
            Assert.Equal(2, s.Pictures.Length);        // 1.2 — rasmlarni solishtirish
            Assert.Equal(3, s.Part12.Length);          // 1.2 — 3 savol
            Assert.Equal(3, s.Part2Questions.Length);  // 2 — 3 savol
            Assert.NotNull(s.Part2Picture);            // 2 — bitta rasm
            Assert.Equal(8, s.Questions().Count);      // jami 8 javob (1–8-savollar)
        }
        Assert.Equal(30, CefrBank.Part11Seconds);
        Assert.Equal(45, CefrBank.Part12FirstSeconds);
        Assert.Equal(30, CefrBank.Part12Seconds);
        Assert.Equal(60, CefrBank.Part2PrepSeconds);
        Assert.Equal(120, CefrBank.Part2SpeakSeconds);
        Assert.Equal(60, CefrBank.Part3PrepSeconds);
        Assert.Equal(120, CefrBank.Part3SpeakSeconds);
    }

    [Fact]
    public void Writing_bank_matches_the_official_new_format()
    {
        Assert.Equal((40, 60), CefrBank.Words11);    // "taxminan 50 so'z"
        Assert.Equal((120, 150), CefrBank.Words12);
        Assert.Equal((180, 200), CefrBank.Words2);
        foreach (var w in CefrBank.Writing)
        {
            Assert.Contains("about 50 words", w.Task11);
            Assert.Contains("friend", w.Task11);
            Assert.Contains("120-150 words", w.Task12);
            Assert.Contains("online discussion", w.Task2);
            Assert.Contains("180-200 words", w.Task2);
        }
    }

    private static PartScore P(int s) => new(s, "because");

    [Fact]
    public void Speaking_result_uses_the_four_official_groups()
    {
        var set = CefrBank.FindSpeaking("c1")!;
        var answers = Enumerable.Range(0, 8).Select(i => new SpokenAnswer(i, i == 7 ? null : [1, 2, 3], "audio/webm", 20)).ToList();
        var ai = new CefrSpeakingAi([new(0, "I like reading."), new(7, "ignored")], P(5), P(5), P(4), P(4), [], "good", ["a", "b", "c", "d"]);
        var r = GeminiCefrEvaluator.BuildSpeakingResult(set, answers, ai);
        Assert.Equal(18, r.Raw);
        Assert.Equal(21, r.RawMax);
        Assert.Equal(64, r.Overall);
        Assert.Equal("B2", r.Level);
        Assert.Equal(new[] { "1–3", "4–6", "7", "8" }, r.Groups.Select(g => g.Questions).ToArray());
        Assert.Equal(6, r.Groups[3].Max);
        Assert.Equal("I like reading.", r.Answers[0].Transcript);
        Assert.Equal("", r.Answers[7].Transcript);
        Assert.Equal(3, r.NextSteps.Count);
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildSpeakingResult(set, answers, ai with { Q8 = P(7) }));
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildSpeakingResult(set, answers, ai with { Q1To3 = P(6) }));
    }

    private static CefrCriteria C => new("tc", "gr", "vo", "co", "pu");

    [Fact]
    public void Writing_result_uses_part1_12_part2_24_and_the_table()
    {
        var set = CefrBank.FindWriting("w1")!;
        var ai = new CefrWritingAi(new(8, "ok", C), new(15, "ok", C), [], "ok", ["x"]);
        var r = GeminiCefrEvaluator.BuildWritingResult(set, "Hi Alex, the club is moving.", "Dear Secretary,", "", 1200, ai);
        Assert.Equal(23, r.Raw);
        Assert.Equal(55, r.Overall);
        Assert.Equal("B2", r.Level);
        Assert.Equal(12, r.Parts[0].Max);
        Assert.Equal(24, r.Parts[1].Max);
        Assert.Equal("pu", r.Parts[1].Criteria.Punctuation);
        Assert.Equal(6, r.Texts[0].Words);
        Assert.Equal(0, r.Texts[2].Words);
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildWritingResult(set, "", "", "", 0, ai with { Part1 = new(13, "x", C) }));
        Assert.Throws<InvalidOperationException>(() => GeminiCefrEvaluator.BuildWritingResult(set, "", "", "", 0, ai with { Part2 = new(25, "x", C) }));
    }

    [Fact]
    public void Prompts_carry_the_official_scale_and_criteria()
    {
        var w = GeminiCefrEvaluator.BuildWritingPrompt(CefrBank.FindWriting("w2")!, "Hi", "", "Post", "ru");
        Assert.Contains("PART 1 (worth 12 points)", w);
        Assert.Contains("PART 2 (worth 24 points)", w);
        Assert.Contains("about 50 words", w);
        Assert.Contains("punctuation and spelling", w);
        Assert.Contains("(no answer)", w);
        Assert.Contains("Russian", w);
        var s = GeminiCefrEvaluator.BuildSpeakingPrompt(CefrBank.FindSpeaking("c2")!, "uz");
        Assert.Contains("Question 8 (Part 3), 0-6", s);
        Assert.Contains("Higher B2", s);
        Assert.Contains("describing it is NOT required", s);
        Assert.Contains("Online learning is better", s);
    }
}
