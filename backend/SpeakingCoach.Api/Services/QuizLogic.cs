namespace SpeakingCoach.Api.Services;

/// <summary>Viktorina uchun juftlik: inglizcha so'z va uning ma'nosi (foydalanuvchi tilida).</summary>
public record QuizPair(string Word, string Meaning);

/// <summary>
/// "Tezkor viktorina" savoli. Direction: "word" — so'z berilgan, ma'nosini
/// topish; "meaning" — ma'no berilgan, so'zni topish. Answer — to'g'ri
/// variant indeksi (mashq — sir emas, brauzer darhol tekshiradi).
/// </summary>
public record QuizQuestion(string Direction, string Prompt, List<string> Options, int Answer);

/// <summary>Viktorina — toza funksiyalar (AI ishlatilmaydi, unit test qilinadi).</summary>
public static class QuizLogic
{
    public const int DefaultCount = 10;
    public const int Options = 4;
    /// <summary>Lug'atda shuncha so'z bo'lmasa — kunlik so'zlar bilan to'ldiriladi.</summary>
    public const int MinPool = 12;

    /// <summary>Takroriy va yaroqsiz juftliklarni tozalaydi (bo'sh, juda uzun, ma'nosi so'zning o'zi).</summary>
    public static List<QuizPair> Clean(IEnumerable<QuizPair> pairs) =>
        pairs
            .Select(p => new QuizPair(p.Word.Trim(), p.Meaning.Trim()))
            .Where(p => p.Word.Length is > 0 and <= 40 && p.Meaning.Length is > 0 and <= 80
                && !string.Equals(p.Word, p.Meaning, StringComparison.OrdinalIgnoreCase))
            .GroupBy(p => p.Word.ToLowerInvariant()).Select(g => g.First())
            .GroupBy(p => p.Meaning.ToLowerInvariant()).Select(g => g.First())
            .ToList();

    /// <summary>Foydalanuvchi lug'ati (avval) + kunlik so'zlar (yetmasa).</summary>
    public static List<QuizPair> Pool(IEnumerable<QuizPair> vocab, string lang)
    {
        var own = Clean(vocab);
        if (own.Count >= MinPool) return own;
        var daily = DailyWords.All.Select(w => new QuizPair(w.Word, lang == "en" ? w.DefinitionEn : w.Translation(lang)));
        return Clean(own.Concat(daily));
    }

    public static List<QuizQuestion> Build(IReadOnlyList<QuizPair> pool, int count, Random rng)
    {
        if (pool.Count < Options) return [];
        var picked = pool.OrderBy(_ => rng.Next()).Take(Math.Min(count, pool.Count)).ToList();
        var questions = new List<QuizQuestion>();
        for (var i = 0; i < picked.Count; i++)
        {
            var p = picked[i];
            var askWord = i % 2 == 0;   // navbatma-navbat: so'z → ma'no, ma'no → so'z
            var wrong = pool.Where(x => x != p).OrderBy(_ => rng.Next()).Take(Options - 1)
                .Select(x => askWord ? x.Meaning : x.Word);
            var options = wrong.Append(askWord ? p.Meaning : p.Word).OrderBy(_ => rng.Next()).ToList();
            questions.Add(new QuizQuestion(
                askWord ? "word" : "meaning",
                askWord ? p.Word : p.Meaning,
                options,
                options.IndexOf(askWord ? p.Meaning : p.Word)));
        }
        return questions;
    }
}
