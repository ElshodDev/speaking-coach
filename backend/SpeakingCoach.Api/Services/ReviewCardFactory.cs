using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>Hali bazaga yozilmagan karta mazmuni.</summary>
public record NewCard(ReviewCardKind Kind, string Front, string Back, string Note);

/// <summary>
/// Mashq natijalaridan takrorlash kartalari yasaydi. G'oya: foydalanuvchi
/// hech narsa qo'shimcha qilmasdan, o'z xatolari avtomatik "eslab qolish
/// ro'yxati"ga tushadi. Toza funksiyalar — bazaga tegmaydi, test qilish oson.
/// </summary>
public static class ReviewCardFactory
{
    public const int MaxFrontLength = 500;
    public const int MaxBackLength = 500;
    public const int MaxNoteLength = 1000;

    /// <summary>Speaking/Writing tuzatishlari: "xato ibora" → "to'g'ri ibora".</summary>
    public static List<NewCard> FromCorrections(IEnumerable<CorrectionItem> corrections)
    {
        var cards = new List<NewCard>();
        foreach (var c in corrections)
        {
            var front = Clean(c.Original, MaxFrontLength);
            var back = Clean(c.Corrected, MaxBackLength);
            // Bo'sh yoki "tuzatish" asl bilan bir xil bo'lsa — o'rganadigan narsa yo'q.
            if (front.Length == 0 || back.Length == 0 || string.Equals(front, back, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            cards.Add(new NewCard(ReviewCardKind.Correction, front, back, Clean(c.Explanation, MaxNoteLength)));
        }
        return Distinct(cards);
    }

    /// <summary>Reading/Listening'dagi xato javoblar: savol → to'g'ri javob.</summary>
    public static List<NewCard> FromWrongAnswers(ComprehensionExercise exercise, ComprehensionResult result)
    {
        var cards = new List<NewCard>();
        for (var i = 0; i < result.Results.Count && i < exercise.Questions.Count; i++)
        {
            if (result.Results[i].IsCorrect) continue;
            var q = exercise.Questions[i];
            var front = Clean($"{q.Question} ({exercise.Title})", MaxFrontLength);
            var back = Clean(q.Options[q.CorrectIndex], MaxBackLength);
            cards.Add(new NewCard(ReviewCardKind.Question, front, back, Clean(q.Explanation, MaxNoteLength)));
        }
        return Distinct(cards);
    }

    public static string Clean(string? text, int maxLength)
    {
        var t = (text ?? "").Trim();
        return t.Length <= maxLength ? t : t[..maxLength];
    }

    // Bitta natija ichida bir xil ibora ikki marta kelsa (Gemini ba'zan
    // takrorlaydi) — bitta karta yetarli.
    private static List<NewCard> Distinct(List<NewCard> cards) =>
        cards.GroupBy(c => c.Front, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
}
