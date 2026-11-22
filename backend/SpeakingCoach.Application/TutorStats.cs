using System.Text.Json;
namespace SpeakingCoach.Application;

/// <summary>Bitta tugatilgan dars (admin statistikasi uchun): kim, mavzu, natija.</summary>
public record LessonRow(Guid? UserId, string? PromptData, string? ResponseData);

public record TutorTopic(string Topic, int Lessons);

/// <summary>AI ustoz bo'yicha admin xulosasi: boshlangan/tugagan darslar, mini-test, o'zlashtirish, mavzular, baho.</summary>
public record TutorStatsResult(
    int Days,
    int Started, int StartedFree, int StartedRecommended, int StartedAssigned,
    int Completed, int Learners, int? CompletionPercent, int? AvgTestPercent,
    int Strong, int Ok, int Weak, int WroteOwn,
    int RatedUp, int RatedDown, int? HelpfulPercent,
    int LessonAssignments,
    List<TutorTopic> TopTopics);

/// <summary>
/// AI ustoz o'lchovlari (admin): hodisalar mavjud Feedbacks jadvalidan (Kind = rating; "lesson_start:*" — server yozadi,
/// "lesson:up|down" — foydalanuvchi bahosi), tugatilgan darslar — Activities'dan. Toza funksiyalar (testlanadi).
/// </summary>
public static class TutorStats
{
    public const string StartPrefix = "lesson_start:";
    public const string RatingUp = "lesson:up";
    public const string RatingDown = "lesson:down";
    public static readonly string[] Sources = ["free", "recommended", "assigned"];
    public const int MaxTopics = 8;

    public static string StartKey(string source) => StartPrefix + (Sources.Contains(source) ? source : "free");

    /// <param name="eventCounts">Kalit → soni (Feedbacks, Kind = rating); boshqa kalitlar e'tiborsiz.</param>
    public static TutorStatsResult Build(int days, IEnumerable<LessonRow> lessons, IReadOnlyDictionary<string, int> eventCounts, int lessonAssignments)
    {
        var rows = lessons.ToList();
        int Count(string key) => eventCounts.TryGetValue(key, out var n) && n > 0 ? n : 0;

        var free = Count(StartKey("free"));
        var recommended = Count(StartKey("recommended"));
        var assigned = Count(StartKey("assigned"));
        var started = free + recommended + assigned;
        var up = Count(RatingUp);
        var down = Count(RatingDown);

        var learners = rows.Where(r => r.UserId is not null).Select(r => r.UserId).Distinct().Count();
        var insights = LessonInsights.Build(learners, rows.Select(r => r.ResponseData ?? ""));

        // Mavzular: registrsiz birlashtiriladi; ko'rinadigan nom — eng ko'p uchragan yozuv.
        var topics = rows
            .Select(r => LessonInsights.TopicOf(r.PromptData)?.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .GroupBy(t => t!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TutorTopic(g.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key, g.Count()))
            .OrderByDescending(t => t.Lessons).ThenBy(t => t.Topic, StringComparer.OrdinalIgnoreCase)
            .Take(MaxTopics)
            .ToList();

        return new TutorStatsResult(
            days, started, free, recommended, assigned,
            insights.Completed, learners,
            // Tugatganlar ulushi: boshlanishlar yo'q bo'lsa yoki hodisa yozuvi yo'q eski darslar bo'lsa 100% dan oshmasin.
            started == 0 ? null : Math.Min(100, (int)Math.Round(100.0 * insights.Completed / started)),
            insights.AvgTestPercent,
            insights.Strong, insights.Ok, insights.Weak, insights.WroteOwn,
            up, down, up + down == 0 ? null : (int)Math.Round(100.0 * up / (up + down)),
            lessonAssignments,
            topics);
    }
}
