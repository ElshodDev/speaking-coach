using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class ReviewSchedulerTests
{
    private static readonly DateTime Now = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ReviewSchedule NewCard = new(Repetitions: 0, IntervalDays: 0, Ease: 2.5, Lapses: 0, DueAtUtc: Now);

    [Fact]
    public void Good_on_new_card_schedules_for_tomorrow()
    {
        var next = ReviewScheduler.Next(NewCard, ReviewGrade.Good, Now);

        Assert.Equal(1, next.Repetitions);
        Assert.Equal(1, next.IntervalDays);
        Assert.Equal(Now.AddDays(1), next.DueAtUtc);
    }

    [Fact]
    public void Repeated_good_grows_interval_1_then_3_then_times_ease()
    {
        var s1 = ReviewScheduler.Next(NewCard, ReviewGrade.Good, Now);
        var s2 = ReviewScheduler.Next(s1, ReviewGrade.Good, Now);
        var s3 = ReviewScheduler.Next(s2, ReviewGrade.Good, Now);

        Assert.Equal(1, s1.IntervalDays);
        Assert.Equal(3, s2.IntervalDays);
        Assert.Equal(7.5, s3.IntervalDays); // 3 × 2.5
    }

    [Fact]
    public void Again_resets_progress_and_brings_card_back_in_minutes()
    {
        var learned = new ReviewSchedule(Repetitions: 4, IntervalDays: 20, Ease: 2.5, Lapses: 0, DueAtUtc: Now);

        var next = ReviewScheduler.Next(learned, ReviewGrade.Again, Now);

        Assert.Equal(0, next.Repetitions);
        Assert.Equal(1, next.Lapses);
        Assert.Equal(2.3, next.Ease, 3);
        Assert.Equal(Now + ReviewScheduler.RelearnDelay, next.DueAtUtc);
    }

    [Fact]
    public void Ease_never_drops_below_minimum()
    {
        var s = NewCard with { Ease = ReviewScheduler.MinEase };

        Assert.Equal(ReviewScheduler.MinEase, ReviewScheduler.Next(s, ReviewGrade.Again, Now).Ease);
        Assert.Equal(ReviewScheduler.MinEase, ReviewScheduler.Next(s, ReviewGrade.Hard, Now).Ease);
    }

    [Fact]
    public void Easy_gives_longer_interval_than_good_and_raises_ease()
    {
        var good = ReviewScheduler.Next(NewCard, ReviewGrade.Good, Now);
        var easy = ReviewScheduler.Next(NewCard, ReviewGrade.Easy, Now);

        Assert.True(easy.IntervalDays > good.IntervalDays);
        Assert.True(easy.Ease > NewCard.Ease);
    }

    [Fact]
    public void Hard_grows_interval_slower_than_good()
    {
        var learned = new ReviewSchedule(Repetitions: 3, IntervalDays: 10, Ease: 2.5, Lapses: 0, DueAtUtc: Now);

        var hard = ReviewScheduler.Next(learned, ReviewGrade.Hard, Now);
        var good = ReviewScheduler.Next(learned, ReviewGrade.Good, Now);

        Assert.Equal(12, hard.IntervalDays);
        Assert.True(hard.IntervalDays < good.IntervalDays);
    }

    [Fact]
    public void Interval_is_capped_at_one_year()
    {
        var old = new ReviewSchedule(Repetitions: 10, IntervalDays: 300, Ease: 3.0, Lapses: 0, DueAtUtc: Now);

        var next = ReviewScheduler.Next(old, ReviewGrade.Easy, Now);

        Assert.Equal(ReviewScheduler.MaxIntervalDays, next.IntervalDays);
    }
}

public class StreakTests
{
    // Toshkent: UTC+5 → JavaScript getTimezoneOffset() = -300.
    private const int Tashkent = -300;
    private static readonly DateTime Now = new(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc); // Toshkentda 15:00

    [Fact]
    public void No_activity_means_zero()
    {
        Assert.Equal(0, ReviewScheduler.ComputeStreak(Array.Empty<DateTime>(), Now, Tashkent));
    }

    [Fact]
    public void Today_and_two_previous_days_is_three()
    {
        var times = new[] { Now, Now.AddDays(-1), Now.AddDays(-2), Now.AddDays(-2).AddHours(1) };

        Assert.Equal(3, ReviewScheduler.ComputeStreak(times, Now, Tashkent));
    }

    [Fact]
    public void Streak_stays_alive_until_the_day_ends_if_yesterday_was_done()
    {
        var times = new[] { Now.AddDays(-1), Now.AddDays(-2) };

        Assert.Equal(2, ReviewScheduler.ComputeStreak(times, Now, Tashkent));
    }

    [Fact]
    public void Gap_breaks_the_streak()
    {
        var times = new[] { Now, Now.AddDays(-2), Now.AddDays(-3) };

        Assert.Equal(1, ReviewScheduler.ComputeStreak(times, Now, Tashkent));
    }

    [Fact]
    public void Days_are_counted_in_local_time_not_utc()
    {
        // 25-sentabr 20:00 UTC = Toshkentda 26-sentabr 01:00. UTC bo'yicha
        // ikkala mashq bitta kunda, lekin foydalanuvchi uchun — ikki xil kunda.
        var lateNightUtc = new DateTime(2026, 9, 25, 20, 0, 0, DateTimeKind.Utc);
        var sameUtcDayEarlier = new DateTime(2026, 9, 25, 8, 0, 0, DateTimeKind.Utc);

        Assert.Equal(2, ReviewScheduler.ComputeStreak(new[] { lateNightUtc, sameUtcDayEarlier }, lateNightUtc, Tashkent));
        Assert.Equal(1, ReviewScheduler.ComputeStreak(new[] { lateNightUtc, sameUtcDayEarlier }, lateNightUtc, 0));
    }
}
