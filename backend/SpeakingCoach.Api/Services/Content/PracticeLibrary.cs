using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Content;

/// <summary>Ilovaning o'z Reading/Listening mashqi (Gemini'siz): original matn va 4 ta savol.</summary>
public record PracticeItem(string Id, string Level, ComprehensionExercise Exercise);

/// <summary>
/// Tayyor mashqlar kutubxonasi: har darajada (A2–C1) Reading va Listening
/// uchun bir nechtadan original mashq. Foydalanuvchi "📚 Tayyor mashq"ni
/// tanlasa, shu yerdan hali ishlamaganini oladi — AI limiti sarflanmaydi.
/// Matnlar Gemini promptidagi qoidalarga mos (daraja bo'yicha so'z soni,
/// 4 ta savol × 4 variant) — ContentTests tekshiradi.
/// </summary>
public static partial class PracticeLibrary
{
    public static IReadOnlyList<PracticeItem> For(ActivityType type) =>
        type == ActivityType.Listening ? ListeningItems : ReadingItems;

    public static PracticeItem? Find(ActivityType type, string? id) =>
        For(type).FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Keyingi mashq: tanlangan darajada hali ishlanmaganlarning birinchisi;
    /// hammasi ishlangan bo'lsa — eng uzoq vaqt oldin ishlangani. Daraja
    /// bo'yicha mashq bo'lmasa — qo'shni darajadan.
    /// </summary>
    public static PracticeItem? Next(ActivityType type, string level, IReadOnlyCollection<string> doneTitlesNewestFirst)
    {
        var all = For(type);
        if (all.Count == 0) return null;
        var order = new[] { level }.Concat(LearnerLevel.All.Where(l => l != level)).ToList();
        foreach (var lv in order)
        {
            var items = all.Where(x => x.Level == lv).ToList();
            if (items.Count == 0) continue;
            var fresh = items.FirstOrDefault(x => !doneTitlesNewestFirst.Contains(x.Exercise.Title));
            if (fresh is not null) return fresh;
            if (lv == level)
            {
                // Hammasi ishlangan: eng eskisi (ro'yxatda eng oxirida turgani) qaytadi.
                var titles = doneTitlesNewestFirst.ToList();
                return items.OrderByDescending(x => titles.IndexOf(x.Exercise.Title)).First();
            }
        }
        return all[0];
    }

    public static (int Done, int Total) Progress(ActivityType type, string level, IReadOnlyCollection<string> doneTitles)
    {
        var items = For(type).Where(x => x.Level == level).ToList();
        return (items.Count(x => doneTitles.Contains(x.Exercise.Title)), items.Count);
    }
}
