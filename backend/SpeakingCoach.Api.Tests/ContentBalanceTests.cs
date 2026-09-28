using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Content;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

/// <summary>
/// To'g'ri javob harfi bir tomonga og'ib ketmasin: aks holda talaba matnni
/// o'qimay "B" ni belgilab ham yaxshi ball oladi. Tayyor mock testlardagi
/// variantli (mcq) savollar va tayyor mashqlar kutubxonasi tekshiriladi.
/// </summary>
public class ContentBalanceTests
{
    // Har harf ulushi chegaralari (variantlar soniga qarab).
    private static (double Min, double Max) Bounds(int options) => options switch
    {
        3 => (0.25, 0.42),
        4 => (0.18, 0.32),
        _ => (0.0, 1.0),
    };

    /// <summary>Imtihon → variantlar soni → [A, B, C, ...] sanoqlari.</summary>
    public static Dictionary<(string Exam, int Options), int[]> McqLetterCounts()
    {
        var result = new Dictionary<(string, int), int[]>();
        foreach (var m in BuiltInMocks.All)
        {
            var groups = m.Content switch
            {
                ReadingTest r => r.Passages.SelectMany(p => p.Groups),
                ListeningTest l => l.Parts.SelectMany(p => p.Groups),
                _ => [],
            };
            foreach (var g in groups.Where(g => g.Type == "mcq"))
            foreach (var q in g.Questions)
            {
                var n = q.Options!.Count;
                if (!result.TryGetValue((m.Exam, n), out var counts)) result[(m.Exam, n)] = counts = new int[n];
                counts[q.Answers[0].Trim().ToUpperInvariant()[0] - 'A']++;
            }
        }
        return result;
    }

    private static string Describe(int[] counts) =>
        string.Join(" ", counts.Select((c, i) => $"{(char)('A' + i)}{c}"));

    [Fact]
    public void Built_in_mcq_answer_letters_are_balanced()
    {
        var problems = new List<string>();
        foreach (var ((exam, options), counts) in McqLetterCounts())
        {
            var total = counts.Sum();
            if (total < options * 5) continue; // juda kam savol — statistika ma'nosiz
            var (min, max) = Bounds(options);
            if (counts.Any(c => (double)c / total < min || (double)c / total > max))
                problems.Add($"{exam} {options}-variant: {Describe(counts)}");
        }
        Assert.True(problems.Count == 0, string.Join(" || ", problems));
    }

    [Theory]
    [InlineData(ActivityType.Reading)]
    [InlineData(ActivityType.Listening)]
    public void Practice_library_answer_positions_are_balanced(ActivityType type)
    {
        var questions = PracticeLibrary.For(type).SelectMany(x => x.Exercise.Questions).ToList();
        foreach (var n in questions.Select(q => q.Options.Count).Distinct())
        {
            var group = questions.Where(q => q.Options.Count == n).ToList();
            if (group.Count < n * 5) continue;
            var counts = new int[n];
            foreach (var q in group) counts[q.CorrectIndex]++;
            var (min, max) = Bounds(n);
            Assert.True(counts.All(c => (double)c / group.Count >= min && (double)c / group.Count <= max),
                $"{type} {n}-variant: {Describe(counts)}");
        }
    }
}
