namespace SpeakingCoach.Domain.Rules;

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

    /// <summary>
    /// Seriyani kichikdan kattaga o'sib boruvchi oynalar bilan hisoblaydi: avval qisqa oyna (odatdagi foydalanuvchida
    /// seriya qisqa — kam qator o'qiladi), seriya oynaning chetiga yetgan (<c>windowDays - 1</c> va undan ko'p) bo'lsa —
    /// keyingi, kattaroq oyna. Natija butun tarix bo'yicha <see cref="ComputeStreak"/> bilan bir xil: kun-kunma-kun
    /// sanash birinchi bo'sh kunda to'xtaydi, oyna chetidagi (to'liq bo'lmagan) kunga esa yetmasdan oldin kengayadi.
    /// </summary>
    /// <param name="loadSince">Berilgan oynada (oxirgi N kun) bo'lgan faoliyat vaqtlarini qaytaradi.</param>
    public static async Task<int> ComputeStreakProgressiveAsync(
        Func<int, Task<IReadOnlyList<DateTime>>> loadSince, DateTime nowUtc, int tzOffsetMinutes, IReadOnlyList<int> windowDays)
    {
        var streak = 0;
        foreach (var window in windowDays)
        {
            streak = ComputeStreak(await loadSince(window), nowUtc, tzOffsetMinutes);
            if (streak < window - 1) break;
        }
        return streak;
    }

    /// <summary>Mahalliy "bugun" kunining UTC chegaralari: [boshi, ertangi boshi).</summary>
    public static (DateTime StartUtc, DateTime EndUtc) LocalDayBoundsUtc(DateTime nowUtc, int tzOffsetMinutes)
    {
        var start = ToLocalDate(nowUtc, tzOffsetMinutes).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddMinutes(tzOffsetMinutes);
        return (start, start.AddDays(1));
    }

    /// <summary>Takroriy baho oynasi: shu vaqt ichidagi ikkinchi baho — ikki marta bosish.</summary>
    public static readonly TimeSpan DuplicateGradeWindow = TimeSpan.FromSeconds(5);

    /// <summary>Muddatidan shuncha oldin baholash ham qabul qilinadi (soatlar farqi, sahifa ochiq qolgani).</summary>
    public static readonly TimeSpan EarlyGradeSlack = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Karta hali navbatda emas (muddati kelmagan) — baho e'tiborsiz qoldiriladi:
    /// aks holda bitta kartani qayta-qayta baholab XP "yig'ish" va o'qituvchining
    /// "N ta karta takrorla" topshirig'ini soxta bajarish mumkin bo'lardi.
    /// </summary>
    public static bool IsTooEarly(DateTime dueAtUtc, DateTime nowUtc) => dueAtUtc - nowUtc > EarlyGradeSlack;

    public static bool IsDuplicateGrade(DateTime? lastReviewedUtc, DateTime nowUtc) =>
        lastReviewedUtc is { } last && nowUtc - last >= TimeSpan.Zero && nowUtc - last < DuplicateGradeWindow;

    public static DateOnly ToLocalDate(DateTime utc, int tzOffsetMinutes) =>
        DateOnly.FromDateTime(utc.AddMinutes(-tzOffsetMinutes));
}
