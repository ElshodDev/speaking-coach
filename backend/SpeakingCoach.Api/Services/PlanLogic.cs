using System.Globalization;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Maqsad sozlamalari (tanishtiruv) va "Bugungi reja" — toza funksiyalar,
/// bazaga tegmaydi (unit test qilinadi).
/// </summary>
public static class PlanLogic
{
    public static readonly string[] Goals = ["ielts", "cefr", "general"];
    public static readonly int[] Minutes = [10, 20, 30, 45];
    public const int DefaultMinutes = 20;
    public static readonly string[] IeltsTargets = ["4.5", "5.0", "5.5", "6.0", "6.5", "7.0", "7.5", "8.0", "8.5", "9.0"];
    public static readonly string[] CefrTargets = ["B1", "B2", "C1"];

    /// <summary>Kunlik mashq aylanishi: har kuni boshqa ko'nikma.</summary>
    public static readonly string[] Skills = ["speaking", "writing", "reading", "listening"];

    public static bool IsValidGoal(string? g) => g is not null && Goals.Contains(g);

    public static bool IsValidMinutes(int? m) => m is null || Minutes.Contains(m.Value);

    /// <summary>Maqsad ball imtihon turiga mos bo'lishi kerak (bo'sh — mumkin).</summary>
    public static bool IsValidTarget(string? goal, string? target) =>
        string.IsNullOrEmpty(target) || goal switch
        {
            "ielts" => IeltsTargets.Contains(target),
            "cefr" => CefrTargets.Contains(target),
            _ => false,
        };

    /// <summary>Imtihon sanasi: bugundan boshlab 2 yil ichida (o'tgan sana — xato).</summary>
    public static bool IsValidExamDate(DateOnly? date, DateOnly today) =>
        date is null || (date.Value >= today && date.Value <= today.AddYears(2));

    public static DateOnly? ParseDate(string? s) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public record Input(
        string? Goal,
        int? DailyMinutes,
        DateOnly? ExamDate,
        DateOnly Today,
        int CardsTotal,
        int CardsDue,
        int ReviewedToday,
        int DailyGoal,
        IReadOnlySet<string> PracticedToday,      // "speaking", "writing", ...
        IReadOnlySet<string> MockedToday,         // "listening", "reading", ... (tanlangan imtihon bo'yicha)
        IReadOnlyDictionary<string, DateTime> LastMockUtc); // modul → oxirgi urinish (tanlangan imtihon)

    public record Item(string Key, string Kind, string Route, bool Done, int Minutes, int? Progress = null, int? Target = null);

    public record Plan(string? Goal, int? DaysToExam, List<Item> Items);

    public static Plan Build(Input i)
    {
        var minutes = i.DailyMinutes ?? DefaultMinutes;
        var items = new List<Item>();

        // 1) Takrorlash — kartalar bo'lsa (xatolar va so'zlar shu yerga tushadi).
        if (i.CardsTotal > 0)
        {
            var target = i.DailyGoal;
            var done = i.ReviewedToday >= target || (i.CardsDue == 0 && i.ReviewedToday > 0);
            items.Add(new Item("review", "review", "review", done, 5, Math.Min(i.ReviewedToday, target), target));
        }

        // 2) Kunning ko'nikmasi (har kuni boshqa), vaqt ko'p bo'lsa — ikkinchisi ham.
        var day = i.Today.DayNumber;
        var practiceCount = minutes >= 30 ? 2 : 1;
        for (var k = 0; k < practiceCount; k++)
        {
            var skill = Skills[(day + k) % Skills.Length];
            items.Add(new Item($"practice:{skill}", "practice", $"practice/{skill}", i.PracticedToday.Contains(skill), 10));
        }

        // 2b) Shadowing — talaffuz uchun qisqa dars (kuniga 20 daqiqa va undan ko'p ajratganlarga).
        if (minutes >= 20)
        {
            items.Add(new Item("shadowing", "shadowing", "shadowing", i.PracticedToday.Contains("shadowing"), 5));
        }

        // 3) Mock — imtihonga tayyorlanayotganlar uchun.
        int? daysToExam = i.ExamDate is { } d && d >= i.Today ? d.DayNumber - i.Today.DayNumber : null;
        if (i.Goal is "ielts" or "cefr")
        {
            var soon = daysToExam is <= 14;
            var weekAgo = i.Today.AddDays(-7);
            var recent = i.LastMockUtc.Values.Any(t => DateOnly.FromDateTime(t) > weekAgo);
            var mockedToday = i.MockedToday.Count > 0;
            // Imtihon yaqin — har kuni bitta bo'lim; aks holda haftasiga bitta (bajarilgan kuni ham ro'yxatda qoladi).
            if (soon || !recent || mockedToday)
            {
                var module = mockedToday
                    ? Skills.First(i.MockedToday.Contains)
                    : NextMockModule(i.LastMockUtc);
                items.Add(new Item($"mock:{i.Goal}:{module}", "mock", MockRoute(i.Goal, module), mockedToday, module is "listening" ? 40 : module is "speaking" ? 15 : 60));
            }
        }

        return new Plan(i.Goal, daysToExam, items);
    }

    /// <summary>Hali topshirilmagan bo'lim, bo'lmasa — eng uzoq vaqt oldin topshirilgani.</summary>
    public static string NextMockModule(IReadOnlyDictionary<string, DateTime> last) =>
        Skills.FirstOrDefault(m => !last.ContainsKey(m)) ?? Skills.OrderBy(m => last[m]).First();

    public static string MockRoute(string goal, string module) => (goal, module) switch
    {
        ("cefr", _) => $"mock/cefr-{module}",
        (_, "reading") => "mock/reading-academic",
        (_, "writing") => "mock/writing-academic",
        _ => $"mock/{module}",
    };

    /// <summary>Faoliyat turi → reja ko'nikmasi (mock alohida hisoblanadi).</summary>
    public static string? SkillOf(ActivityType type) => type switch
    {
        ActivityType.Speaking => "speaking",
        ActivityType.Writing => "writing",
        ActivityType.Reading => "reading",
        ActivityType.Listening => "listening",
        ActivityType.Shadowing => "shadowing",
        // AI suhbat — gapirish mashqi, diktant — tinglash mashqi sifatida rejada belgilanadi.
        ActivityType.Conversation => "speaking",
        ActivityType.Dictation => "listening",
        _ => null,
    };
}
