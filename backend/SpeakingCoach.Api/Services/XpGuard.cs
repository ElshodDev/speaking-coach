using System.Text.Json;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// AI'siz natijalar (mock Listening/Reading, diktant) bilan XP "yig'ishning"
/// oldini olish — sxemani o'zgartirmasdan: XP Activities qatorlaridan
/// hisoblanadi (ProgressCalculator), shuning uchun cheklov — yangi qator
/// yaratmaslik. Qoidalar toza funksiyalar (testlangan).
/// </summary>
public static class XpGuard
{
    /// <summary>Aynan bir xil javoblar shu vaqt ichida qayta yuborilsa — yangi natija yaratilmaydi.</summary>
    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);

    /// <summary>Mock Listening/Reading natijalari: Toshkent kuniga ko'pi bilan.</summary>
    public const int ObjectiveMocksPerDay = 30;

    /// <summary>Diktant: XP beradigan natijalar Toshkent kuniga ko'pi bilan.</summary>
    public const int DictationsPerDay = 30;

    public const int DictationMaxSentences = 50;

    /// <summary>Diktant gapida ko'pi bilan shuncha so'z (haqiqiy gaplar 5–25 so'z).</summary>
    public const int DictationMaxWordsPerSentence = 40;

    /// <summary>Diktant natijasi mantiqan to'g'rimi: 0 ≤ to'g'ri ≤ jami ≤ gaplar × 40.</summary>
    public static bool ValidDictation(int sentences, int correctWords, int totalWords) =>
        sentences is >= 1 and <= DictationMaxSentences
        && totalWords >= sentences
        && totalWords <= sentences * DictationMaxWordsPerSentence
        && correctWords >= 0
        && correctWords <= totalWords;

    /// <summary>Javoblar "barmoq izi": savol raqami va berilgan javob (katta-kichik harf va bo'shliqlarsiz).</summary>
    public static string Fingerprint(IEnumerable<QuestionReview> review) =>
        string.Join("\n", review.OrderBy(q => q.Number).Select(q => $"{q.Number}={Normalize(q.Given)}"));

    /// <summary>Saqlangan natija (ResponseData) — "questions"[].number/given dan barmoq izi; o'qib bo'lmasa null.</summary>
    public static string? FingerprintOf(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("questions", out var qs) || qs.ValueKind != JsonValueKind.Array) return null;
            var items = new List<(int Number, string Given)>();
            foreach (var q in qs.EnumerateArray())
            {
                if (q.ValueKind != JsonValueKind.Object || !q.TryGetProperty("number", out var n) || !n.TryGetInt32(out var number)) return null;
                var given = q.TryGetProperty("given", out var g) && g.ValueKind == JsonValueKind.String ? g.GetString() ?? "" : "";
                items.Add((number, given));
            }
            return string.Join("\n", items.OrderBy(i => i.Number).Select(i => $"{i.Number}={Normalize(i.Given)}"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public record RecentResult(Guid Id, DateTime CreatedAtUtc, string SetId, string Module, string? ResponseJson);

    /// <summary>
    /// Shu test (setId, module) uchun oxirgi 10 daqiqada aynan shu javoblar bilan
    /// saqlangan natija bo'lsa — uning Id'si (yangi qator yaratilmaydi).
    /// </summary>
    public static Guid? FindDuplicate(IEnumerable<RecentResult> recent, string setId, string module, string fingerprint, DateTime nowUtc) =>
        recent
            .Where(r => r.SetId == setId && r.Module == module && r.CreatedAtUtc > nowUtc - DuplicateWindow && r.ResponseJson is not null)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefault(r => FingerprintOf(r.ResponseJson!) == fingerprint)?.Id;

    private static string Normalize(string? s) =>
        string.Join(' ', (s ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
