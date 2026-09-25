using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

public enum VocabStatus
{
    /// <summary>Hali bir marta ham takrorlanmagan.</summary>
    New,

    /// <summary>Takrorlanyapti, lekin oraliq hali qisqa.</summary>
    Learning,

    /// <summary>Oraliq 3 haftadan oshgan — so'z uzoq muddatli xotirada.</summary>
    Known,
}

/// <summary>
/// Lug'at — alohida jadval emas, balki takrorlash kartalarining "so'z"
/// turidagilari (matndan bosib qo'shilgan va qo'lda qo'shilgan). Shuning
/// uchun lug'atdagi har bir so'z avtomatik ravishda oraliqli takrorlashga
/// ham tushadi. Bu klass — bazaga tegmaydigan sof qoidalar (testlanadi).
/// </summary>
public static class Vocabulary
{
    /// <summary>Shu oraliqdan (kun) oshgan so'z "yodlangan" hisoblanadi.</summary>
    public const double KnownAfterDays = 21;

    public static readonly ReviewCardKind[] Kinds = { ReviewCardKind.Word, ReviewCardKind.Manual };

    public static bool IsVocab(ReviewCardKind kind) => kind is ReviewCardKind.Word or ReviewCardKind.Manual;

    public static VocabStatus StatusOf(DateTime? lastReviewedAtUtc, double intervalDays) =>
        lastReviewedAtUtc is null ? VocabStatus.New
        : intervalDays >= KnownAfterDays ? VocabStatus.Known
        : VocabStatus.Learning;

    /// <summary>
    /// Kartaning izoh qismi: "(noun) a substance used to treat illness — Take this medicine twice a day."
    /// Takrorlashda ham, Lug'at ro'yxatida ham shu matn ko'rinadi.
    /// </summary>
    public static string ComposeNote(string? partOfSpeech, string? definition, string? example)
    {
        var pos = ReviewCardFactory.Clean(partOfSpeech, 30);
        var def = ReviewCardFactory.Clean(definition, 300);
        var ex = ReviewCardFactory.Clean(example, 300);

        var head = pos.Length > 0 && def.Length > 0 ? $"({pos}) {def}" : pos.Length > 0 ? $"({pos})" : def;
        var note = head.Length > 0 && ex.Length > 0 ? $"{head} — {ex}" : head + ex;
        return ReviewCardFactory.Clean(note, ReviewCardFactory.MaxNoteLength);
    }

    /// <summary>"Reading"/"Listening" — so'z qaysi mashqda uchragani; boshqa qiymat — null.</summary>
    public static ActivityType? ParseSource(string? source) =>
        Enum.TryParse<ActivityType>(source, ignoreCase: true, out var t) && t is ActivityType.Reading or ActivityType.Listening
            ? t
            : null;
}
