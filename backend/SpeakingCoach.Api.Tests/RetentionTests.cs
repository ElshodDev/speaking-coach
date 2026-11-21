public class RetentionTests
{
    private static readonly DateOnly Today = new(2026, 11, 20);
    private static Guid Id() => Guid.NewGuid();

    [Fact]
    public void D1_and_D7_count_only_activity_on_exactly_that_day()
    {
        var a = Id(); var b = Id(); var c = Id();
        var signup = new DateOnly(2026, 11, 10);
        var users = new[] { (a, signup), (b, signup), (c, signup) };
        var active = new Dictionary<Guid, HashSet<DateOnly>>
        {
            [a] = [signup, signup.AddDays(1), signup.AddDays(7)],   // D1 va D7
            [b] = [signup.AddDays(1), signup.AddDays(3)],           // faqat D1
            [c] = [signup, signup.AddDays(2)],                      // hech biri
        };
        var cohort = Assert.Single(RetentionCalculator.Cohorts(users, active, Today));
        Assert.Equal(signup, cohort.Day);
        Assert.Equal(3, cohort.Users);
        Assert.Equal(2, cohort.D1);
        Assert.Equal(1, cohort.D7);
    }

    [Fact]
    public void Unfinished_days_are_not_measured()
    {
        var a = Id(); var b = Id();
        var users = new[] { (a, Today), (b, Today.AddDays(-1)), (Id(), Today.AddDays(-7)) };
        var active = new Dictionary<Guid, HashSet<DateOnly>> { [b] = [Today] };
        var cohorts = RetentionCalculator.Cohorts(users, active, Today);
        // Bugun ro'yxatdan o'tgan: D1 ham, D7 ham hali yo'q. Kecha: D1 — bugun hali tugamagan.
        Assert.Null(cohorts.Single(c => c.Day == Today).D1);
        Assert.Null(cohorts.Single(c => c.Day == Today.AddDays(-1)).D1);
        // 7 kun oldin: D1 o'lchanadi, D7 = bugun hali tugamagan.
        var old = cohorts.Single(c => c.Day == Today.AddDays(-7));
        Assert.Equal(0, old.D1);
        Assert.Null(old.D7);
    }

    [Fact]
    public void Cohorts_are_newest_first_and_summary_weights_by_users()
    {
        var u1 = Enumerable.Range(0, 4).Select(_ => Id()).ToArray();   // 4 kishi, 2 tasi qaytgan
        var u2 = Enumerable.Range(0, 6).Select(_ => Id()).ToArray();   // 6 kishi, 1 tasi qaytgan
        var d1 = new DateOnly(2026, 11, 1); var d2 = new DateOnly(2026, 11, 2);
        var users = u1.Select(x => (x, d1)).Concat(u2.Select(x => (x, d2))).ToList();
        var active = new Dictionary<Guid, HashSet<DateOnly>>
        {
            [u1[0]] = [d1.AddDays(1)], [u1[1]] = [d1.AddDays(1)],
            [u2[0]] = [d2.AddDays(1)],
        };
        var cohorts = RetentionCalculator.Cohorts(users, active, Today);
        Assert.Equal([d2, d1], cohorts.Select(c => c.Day).ToArray());
        var s = RetentionCalculator.Summarize(cohorts);
        Assert.Equal(10, s.D1Users);
        Assert.Equal(3, s.D1Retained);
        Assert.Equal(30.0m, s.D1Percent);
        Assert.Equal(10, s.D7Users);
        Assert.Equal(0.0m, s.D7Percent);
    }

    [Fact]
    public void Nothing_measurable_gives_null_percent_not_zero()
    {
        var s = RetentionCalculator.Summarize(RetentionCalculator.Cohorts([(Id(), Today)], new Dictionary<Guid, HashSet<DateOnly>>(), Today));
        Assert.Null(s.D1Percent);
        Assert.Null(s.D7Percent);
        Assert.Empty(RetentionCalculator.Cohorts([], new Dictionary<Guid, HashSet<DateOnly>>(), Today));
    }
}
