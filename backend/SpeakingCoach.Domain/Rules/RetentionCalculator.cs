namespace SpeakingCoach.Domain.Rules;

/// <summary>Bir kunda ro'yxatdan o'tganlar guruhi va ularning qaytishi.</summary>
/// <param name="D1">Ertasi kuni (kun+1) faol bo'lganlar; null — hali o'lchab bo'lmaydi (kun hali tugamagan).</param>
/// <param name="D7">7-kuni (kun+7) faol bo'lganlar; null — hali o'lchab bo'lmaydi.</param>
public record RetentionCohort(DateOnly Day, int Users, int? D1, int? D7);

public record RetentionSummary(int D1Users, int D1Retained, int D7Users, int D7Retained)
{
    /// <summary>Foiz (0–100, bir o'nlik); o'lchanadigan odam bo'lmasa — null.</summary>
    public decimal? D1Percent => D1Users == 0 ? null : Math.Round(100m * D1Retained / D1Users, 1, MidpointRounding.AwayFromZero);
    public decimal? D7Percent => D7Users == 0 ? null : Math.Round(100m * D7Retained / D7Users, 1, MidpointRounding.AwayFromZero);
}

/// <summary>
/// D1/D7 retention — toza hisob (bazasiz, testlanadi). Ta'rif ("N-kun retention"):
/// foydalanuvchi ro'yxatdan o'tgan kunidan keyin aynan N-kuni biror mashq qilgan yoki karta takrorlagan bo'lsa — qaytgan.
/// Kunlar Toshkent vaqti bilan (ilovaning kunlik limitlari bilan bir xil). Tugamagan kun hisobga olinmaydi.
/// </summary>
public static class RetentionCalculator
{
    public static List<RetentionCohort> Cohorts(
        IEnumerable<(Guid UserId, DateOnly SignupDay)> users,
        IReadOnlyDictionary<Guid, HashSet<DateOnly>> activeDays,
        DateOnly today)
    {
        return users
            .GroupBy(u => u.SignupDay)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                int Retained(int n) => g.Count(u => activeDays.TryGetValue(u.UserId, out var days) && days.Contains(g.Key.AddDays(n)));
                // N-kun tugagan bo'lsa (bugundan oldin) — o'lchanadi.
                int? Measure(int n) => g.Key.AddDays(n) < today ? Retained(n) : null;
                return new RetentionCohort(g.Key, g.Count(), Measure(1), Measure(7));
            })
            .ToList();
    }

    public static RetentionSummary Summarize(IEnumerable<RetentionCohort> cohorts)
    {
        int d1u = 0, d1r = 0, d7u = 0, d7r = 0;
        foreach (var c in cohorts)
        {
            if (c.D1 is { } a) { d1u += c.Users; d1r += a; }
            if (c.D7 is { } b) { d7u += c.Users; d7r += b; }
        }
        return new RetentionSummary(d1u, d1r, d7u, d7r);
    }
}
