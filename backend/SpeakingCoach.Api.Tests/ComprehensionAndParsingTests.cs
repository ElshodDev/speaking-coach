using System.Text.Json;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class ComprehensionTests
{
    private static string Question(int correct) =>
        $"{{\"question\":\"Q\",\"options\":[\"a\",\"b\",\"c\",\"d\"],\"correctIndex\":{correct},\"explanation\":\"because\"}}";

    private static string ExerciseJson(params int[] correct) =>
        $"{{\"title\":\"T\",\"passage\":\"P\",\"questions\":[{string.Join(",", correct.Select(Question))}]}}";

    private static ComprehensionExercise Parse(string json) => GeminiClient.DeserializeStrict<ComprehensionExercise>(json);

    [Fact]
    public void Grade_counts_correct_answers_and_reports_the_right_option()
    {
        var exercise = Parse(ExerciseJson(0, 1, 2, 3));

        var result = GeminiComprehensionService.Grade(exercise, new[] { 0, 1, 0, 0 });

        Assert.Equal(2, result.Score);
        Assert.Equal(4, result.Total);
        Assert.False(result.Results[2].IsCorrect);
        Assert.Equal(2, result.Results[2].CorrectIndex);
    }

    [Fact]
    public void Grade_rejects_wrong_number_of_answers_or_out_of_range_option()
    {
        var exercise = Parse(ExerciseJson(0, 1, 2, 3));

        Assert.Throws<ArgumentException>(() => GeminiComprehensionService.Grade(exercise, new[] { 0, 1 }));
        Assert.Throws<ArgumentException>(() => GeminiComprehensionService.Grade(exercise, new[] { 0, 1, 7, 0 }));
    }

    [Fact]
    public void Validate_rejects_exercises_that_break_the_contract()
    {
        Assert.Throws<InvalidOperationException>(() => GeminiComprehensionService.Validate(Parse(ExerciseJson(0, 1, 2))));
        Assert.Throws<InvalidOperationException>(() => GeminiComprehensionService.Validate(Parse(ExerciseJson(0, 1, 2, 4))));
        GeminiComprehensionService.Validate(Parse(ExerciseJson(0, 1, 2, 3))); // to'g'risi xato bermaydi
    }

    [Fact]
    public void Stored_payload_round_trips_through_strict_parsing()
    {
        var exercise = Parse(ExerciseJson(0, 1, 2, 3));

        var roundTrip = Parse(JsonSerializer.Serialize(exercise));

        Assert.Equal(3, roundTrip.Questions[3].CorrectIndex);
    }
}

public class GeminiParsingTests
{
    private static string Writing(string grammar) =>
        "{\"taskAchievement\":{\"score\":70,\"reasoning\":\"a\"},\"coherenceCohesion\":{\"score\":60,\"reasoning\":\"b\"},"
        + "\"grammar\":" + grammar + ",\"vocabulary\":{\"score\":50,\"reasoning\":\"d\"},"
        + "\"topCorrections\":[],\"encouragement\":\"e\",\"nextFocus\":\"f\"}";

    private static void ParseAndCheck(string json)
    {
        var r = GeminiClient.DeserializeStrict<WritingEvaluationResult>(json);
        ScoreGuard.EnsureValid(
            ("taskAchievement", r.TaskAchievement), ("coherenceCohesion", r.CoherenceCohesion),
            ("grammar", r.Grammar), ("vocabulary", r.Vocabulary));
    }

    [Fact]
    public void Valid_reply_is_accepted_including_a_genuine_zero()
    {
        ParseAndCheck(Writing("{\"score\":65,\"reasoning\":\"c\"}"));
        ParseAndCheck(Writing("{\"score\":0,\"reasoning\":\"off-topic\"}"));
    }

    [Theory]
    [InlineData("{\"value\":65,\"reasoning\":\"c\"}")] // "score" yo'q — eski kodda jimgina 0 bo'lardi
    [InlineData("{\"score\":65,\"reasoning\":null}")]  // izoh null
    [InlineData("{\"score\":150,\"reasoning\":\"c\"}")] // oraliqdan tashqari
    [InlineData("{\"score\":65,\"reasoning\":\"  \"}")] // bo'sh izoh
    public void Malformed_or_nonsensical_reply_is_rejected(string grammar)
    {
        Assert.Throws<InvalidOperationException>(() => ParseAndCheck(Writing(grammar)));
    }

    [Fact]
    public void Non_json_reply_is_rejected_with_readable_error()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ParseAndCheck("Sorry, I can't help with that"));

        Assert.Contains("kutilgan shaklda emas", ex.Message);
    }
}

public class AuthHelpersTests
{
    [Fact]
    public void Token_hash_is_64_hex_chars_and_deterministic()
    {
        var h = AuthService.HashToken("abc");

        Assert.Equal(64, h.Length);
        Assert.Equal(h, AuthService.HashToken("abc"));
        Assert.NotEqual(h, AuthService.HashToken("abd"));
    }

    [Fact]
    public void Email_is_normalized_for_lookup()
    {
        Assert.Equal("ali@mail.com", AuthService.NormalizeEmail("  Ali@Mail.COM "));
    }
}
