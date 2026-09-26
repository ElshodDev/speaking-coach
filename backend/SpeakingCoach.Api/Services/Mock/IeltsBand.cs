using System.Text.RegularExpressions;

namespace SpeakingCoach.Api.Services.Mock;

/// <summary>
/// IELTS ballari bilan hisob-kitob (ielts.org, "IELTS scoring in detail"):
/// - Speaking: 4 mezon teng ulushli, natija — o'rtachasi;
/// - Writing: har vazifada 4 mezon teng ulushli; Task 2 ning ulushi ko'proq
///   (rasmiy sahifa aniq koeffitsiyent bermaydi — biz ielts.org'ning
///   Writing format sahifasidagi "twice as much" qoidasini ishlatamiz);
/// - yaxlitlash: rasmiy .25/.75 qoidasi (6.25 → 6.5, 6.75 → 7.0) ielts.org'da
///   UMUMIY ball uchun yozilgan; bo'lim ichidagi yaxlitlash e'lon qilinmagan,
///   shuning uchun biz ham shu qoidani qo'llaymiz. Natija — TAXMINIY band.
/// </summary>
public static partial class IeltsBand
{
    public static decimal RoundHalfBand(decimal value)
    {
        var clamped = Math.Clamp(value, 0m, 9m);
        return Math.Floor(clamped * 2m + 0.5m) / 2m;
    }

    /// <summary>Speaking: 4 mezonning o'rtachasi (teng ulush).</summary>
    public static decimal Speaking(int fluency, int lexical, int grammar, int pronunciation) =>
        RoundHalfBand((fluency + lexical + grammar + pronunciation) / 4m);

    /// <summary>Bitta Writing vazifasi: 4 mezonning o'rtachasi (yaxlitlanmagan — umumiy hisob uchun).</summary>
    public static decimal TaskRaw(int task, int coherence, int lexical, int grammar) =>
        (task + coherence + lexical + grammar) / 4m;

    /// <summary>
    /// Writing umumiy: Task 2 ning ulushi Task 1 dan ikki barobar (rasmiy).
    /// Yaxlitlash oxirida, bir marta — oraliq yaxlitlash natijani siljitmasligi uchun.
    /// </summary>
    public static decimal Writing(decimal task1Raw, decimal task2Raw) =>
        RoundHalfBand((task1Raw + 2m * task2Raw) / 3m);

    /// <summary>So'z soni: harf yoki raqami bor bo'laklar ("-", "•" kabi belgilar sanalmaydi).</summary>
    public static int CountWords(string? text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : WordRegex().Matches(text).Count;

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)*")]
    private static partial Regex WordRegex();
}
