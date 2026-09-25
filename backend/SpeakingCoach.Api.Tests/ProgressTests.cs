using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class XpAndLevelTests
{
    private static readonly DateTime T = new(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Xp_rules_per_activity_type_and_review()
    {
        var acts = new[]
        {
            new ActivityFact(ActivityType.Speaking, T),              // 20
            new ActivityFact(ActivityType.Writing, T),               // 20
            new ActivityFact(ActivityType.Reading, T, 3),            // 15 + 3×5 = 30
            new ActivityFact(ActivityType.Listening, T, 0),          // 15
        };

        Assert.Equal(20 + 20 + 30 + 15 + 10 * 2, ProgressCalculator.TotalXp(acts, reviewCount: 10));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(299, 2)]
    [InlineData(300, 3)]
    [InlineData(1000, 5)]
    public void Level_thresholds_grow_by_100_each_level(int xp, int expectedLevel)
    {
        Assert.Equal(expectedLevel, ProgressCalculator.LevelFor(xp).Level);
    }

    [Fact]
    public void Level_info_gives_bounds_for_the_progress_bar()
    {
        var info = ProgressCalculator.LevelFor(350);

        Assert.Equal(3, info.Level);
        Assert.Equal(300, info.LevelStartXp);
        Assert.Equal(600, info.NextLevelXp);
    }

    [Fact]
    public void Correct_answers_are_read_from_stored_result_json()
    {
        Assert.Equal(3, ProgressCalculator.ReadCorrectAnswers(ActivityType.Reading, "{\"score\":3,\"total\":4}"));
        Assert.Equal(0, ProgressCalculator.ReadCorrectAnswers(ActivityType.Listening, "not json"));
        Assert.Null(ProgressCalculator.ReadCorrectAnswers(ActivityType.Writing, "{\"score\":3}"));
    }
}

public class BadgeAndStreakTests
{
    private static readonly DateTime T = new(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);

    private static bool Earned(IReadOnlyList<Badge> badges, string id) => badges.Single(b => b.Id == id).Earned;

    [Fact]
    public void Best_streak_is_the_longest_run_not_the_current_one()
    {
        var d = new DateOnly(2026, 9, 1);
        var days = new[] { d, d.AddDays(1), d.AddDays(2), d.AddDays(3), /* gap */ d.AddDays(10), d.AddDays(11), d.AddDays(1) };

        Assert.Equal(4, ProgressCalculator.BestStreak(days));
        Assert.Equal(0, ProgressCalculator.BestStreak(Array.Empty<DateOnly>()));
    }

    [Fact]
    public void Nothing_is_earned_before_the_first_exercise()
    {
        var badges = ProgressCalculator.Badges(Array.Empty<ActivityFact>(), 0, 0);

        Assert.DoesNotContain(badges, b => b.Earned);
        Assert.Equal(10, badges.Count);
    }

    [Fact]
    public void Badges_unlock_at_their_thresholds()
    {
        var acts = new List<ActivityFact>
        {
            new(ActivityType.Speaking, T), new(ActivityType.Writing, T),
            new(ActivityType.Reading, T, 4), new(ActivityType.Listening, T, 2),
        };

        var badges = ProgressCalculator.Badges(acts, reviewCount: 50, bestStreak: 7);

        Assert.True(Earned(badges, "first_step"));
        Assert.True(Earned(badges, "all_rounder"));
        Assert.True(Earned(badges, "perfect"));
        Assert.True(Earned(badges, "streak_7"));
        Assert.False(Earned(badges, "streak_30"));
        Assert.True(Earned(badges, "reviewer_50"));
        Assert.False(Earned(badges, "reviewer_500"));
        Assert.False(Earned(badges, "writer_10"));
    }

    [Fact]
    public void Calendar_has_one_entry_per_local_day_including_empty_days()
    {
        var now = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        var events = new[] { now, now.AddHours(-1), now.AddDays(-2), new DateTime(2026, 9, 24, 20, 0, 0, DateTimeKind.Utc) };

        var cal = ProgressCalculator.Calendar(events, now, tzOffsetMinutes: -300, days: 7);

        Assert.Equal(7, cal.Count);
        Assert.Equal(new DateOnly(2026, 9, 25), cal[^1].Day);
        // 24-sentabr 20:00 UTC — Toshkentda 25-sentabr 01:00, ya'ni "bugun".
        Assert.Equal(3, cal[^1].Count);
        Assert.Equal(1, cal[^3].Count);
        Assert.Equal(0, cal[0].Count);
    }
}

public class LeaderboardTests
{
    [Fact]
    public void Ties_share_a_rank_and_zero_xp_is_hidden()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid(); var d = Guid.NewGuid();

        var ranked = ProgressCalculator.Rank(new[]
        {
            new LeaderboardRow(a, "Aziza", 120),
            new LeaderboardRow(b, "Bekzod", 200),
            new LeaderboardRow(c, "Chori", 120),
            new LeaderboardRow(d, "Dilnoza", 0),
        });

        Assert.Equal(3, ranked.Count);
        Assert.Equal(("Bekzod", 1), (ranked[0].Name, ranked[0].Rank));
        Assert.Equal(("Aziza", 2), (ranked[1].Name, ranked[1].Rank));
        Assert.Equal(("Chori", 2), (ranked[2].Name, ranked[2].Rank));
    }

    [Theory]
    [InlineData("2026-09-24", "2026-09-21")] // payshanba -> dushanba
    [InlineData("2026-09-21", "2026-09-21")] // dushanbaning o'zi
    [InlineData("2026-09-27", "2026-09-21")] // yakshanba hali shu hafta
    public void Week_starts_on_monday_utc(string day, string expectedMonday)
    {
        var start = ProgressCalculator.WeekStartUtc(DateTime.Parse(day).AddHours(15));

        Assert.Equal(DateTime.Parse(expectedMonday), start);
    }

    [Theory]
    [InlineData("aziza@mail.com", "az***@mail.com")]
    [InlineData("a@x.uz", "a***@x.uz")]
    [InlineData("broken", "***")]
    public void Emails_are_masked_for_the_admin_panel(string email, string expected)
    {
        Assert.Equal(expected, ProgressCalculator.MaskEmail(email));
    }
}

public class LevelAndPromptTests
{
    [Theory]
    [InlineData("b2", "B2")]
    [InlineData(" C1 ", "C1")]
    [InlineData("", "B1")]
    [InlineData(null, "B1")]
    [InlineData("Z9", "B1")]
    public void Level_is_normalized_with_b1_default(string? input, string expected)
    {
        Assert.Equal(expected, LearnerLevel.Normalize(input));
    }

    [Fact]
    public void Every_prompt_template_formats_and_contains_the_level()
    {
        foreach (var level in LearnerLevel.All)
        {
            var d = LearnerLevel.Describe(level);
            Assert.Contains(d, GeminiSpeakingService.BuildPrompt("my city", level));
            Assert.Contains(d, GeminiWritingService.BuildPrompt("topic", "essay", level));
            Assert.Contains(d, GeminiWordService.BuildPrompt("bank", "We sat on the river bank.", level));

            var reading = GeminiComprehensionService.BuildPrompt(ActivityType.Reading, level, "tea");
            var (min, max) = LearnerLevel.ReadingWords(level);
            Assert.Contains(d, reading);
            Assert.Contains($"{min}-{max} words", reading);

            var listening = GeminiComprehensionService.BuildPrompt(ActivityType.Listening, level, "tea");
            Assert.Contains($"{LearnerLevel.ListeningWords(level).Min}-{LearnerLevel.ListeningWords(level).Max} words", listening);
        }
    }

    [Theory]
    [InlineData("bank", "bank")]
    [InlineData("  well-known, ", "well-known")]
    [InlineData("don't", "don't")]
    public void Word_input_is_cleaned(string input, string expected)
    {
        var clean = GeminiWordService.Sanitize(input, "A sentence.", out var error);

        Assert.Null(error);
        Assert.Equal(expected, clean!.Value.Word);
    }

    [Theory]
    [InlineData("<script>")]
    [InlineData("two words")]
    [InlineData("")]
    public void Suspicious_word_input_is_rejected(string input)
    {
        var clean = GeminiWordService.Sanitize(input, "x", out var error);

        Assert.Null(clean);
        Assert.NotNull(error);
    }
}
