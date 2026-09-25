namespace SpeakingCoach.Api.Services;

/// <summary>Foydalanuvchi kartani qanchalik eslaganini o'zi baholaydi.</summary>
public enum ReviewGrade
{
    Again = 0, // eslay olmadim
    Hard = 1,  // qiynalib esladim
    Good = 2,  // esladim
    Easy = 3,  // juda oson edi
}

/// <summary>Kartaning jadval holati — ReviewCard'dagi tegishli maydonlar.</summary>
public record ReviewSchedule(int Repetitions, double IntervalDays, double Ease, int Lapses, DateTime DueAtUtc);

/// <summary>
/// Oraliqli takrorlash algoritmi — SuperMemo-2 (SM-2) ning soddalashtirilgan
/// varianti (Anki ham shunga asoslangan). Bazaga ham, HTTP'ga ham bog'liq
/// emas: faqat kirish → chiqish. Shuning uchun uni to'liq unit test bilan
/// tekshirish oson (SpeakingCoach.Api.Tests/ReviewSchedulerTests.cs).
/// </summary>
public static class ReviewScheduler
{
    public const double MinEase = 1.3;
    public const double MaxIntervalDays = 365;

    /// <summary>"Eslay olmadim" bosilganda karta shu vaqtdan keyin qayta chiqadi.</summary>
    public static readonly TimeSpan RelearnDelay = TimeSpan.FromMinutes(10);

    public static ReviewSchedule Next(ReviewSchedule s, ReviewGrade grade, DateTime nowUtc)
    {
        if (grade == ReviewGrade.Again)
        {
            // Unutildi: boshidan o'rganiladi, lekin "osonlik" ham pasayadi —
            // tez-tez unutiladigan karta keyinchalik ham tezroq qaytadi.
            return new ReviewSchedule(
                Repetitions: 0,
                IntervalDays: 0,
                Ease: Math.Max(MinEase, s.Ease - 0.2),
                Lapses: s.Lapses + 1,
                DueAtUtc: nowUtc + RelearnDelay);
        }

        double interval;
        var ease = s.Ease;
        switch (grade)
        {
            case ReviewGrade.Hard:
                interval = s.Repetitions == 0 ? 1 : Math.Max(1, s.IntervalDays * 1.2);
                ease = Math.Max(MinEase, ease - 0.15);
                break;
            case ReviewGrade.Good:
                interval = s.Repetitions switch
                {
                    0 => 1,
                    1 => 3,
                    _ => s.IntervalDays * ease,
                };
                break;
            case ReviewGrade.Easy:
                interval = s.Repetitions == 0 ? 4 : s.IntervalDays * ease * 1.3;
                ease += 0.15;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
        }

        interval = Math.Round(Math.Min(interval, MaxIntervalDays), 2);
        return new ReviewSchedule(s.Repetitions + 1, interval, ease, s.Lapses, nowUtc.AddDays(interval));
    }

    /// <summary>
    /// Ketma-ket necha kun mashq qilingan (streak). Kunlar foydalanuvchining
    /// MAHALLIY vaqti bo'yicha hisoblanadi: Toshkentda soat 02:00 da qilingan
    /// mashq UTC bo'yicha "kechagi kun" bo'lib qolmasligi kerak.
    ///
    /// tzOffsetMinutes — JavaScript'ning `new Date().getTimezoneOffset()`
    /// qiymati (UTC − mahalliy vaqt, daqiqada; Toshkent uchun −300).
    ///
    /// Bugun hali mashq qilinmagan bo'lsa, streak kechagidan boshlab
    /// sanaladi — kun tugaguncha u "yonib" turadi.
    /// </summary>
    public static int ComputeStreak(IEnumerable<DateTime> activityUtc, DateTime nowUtc, int tzOffsetMinutes)
    {
        var days = activityUtc.Select(t => ToLocalDate(t, tzOffsetMinutes)).ToHashSet();
        var day = ToLocalDate(nowUtc, tzOffsetMinutes);
        if (!days.Contains(day))
        {
            day = day.AddDays(-1);
        }

        var streak = 0;
        while (days.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    public static DateOnly ToLocalDate(DateTime utc, int tzOffsetMinutes) =>
        DateOnly.FromDateTime(utc.AddMinutes(-tzOffsetMinutes));
}
