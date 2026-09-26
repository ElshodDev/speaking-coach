using System.Globalization;
using System.Text.Json;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>O'quvchining bitta urinishi — vazifa bajarilganini aniqlash uchun kerakli qismi.</summary>
public record ActivityRef(Guid Id, ActivityType Type, DateTime CreatedAtUtc, string? Exam, string? Module, string? Score);

/// <summary>Vazifa holati bitta o'quvchi uchun.</summary>
public record AssignmentStatus(bool Done, bool Late, DateTime? DoneAtUtc, Guid? ActivityId, string? Score, int? Progress, int? Target);

/// <summary>
/// Guruh va vazifalar qoidalari — toza funksiyalar (unit test qilinadi).
/// </summary>
public static class GroupLogic
{
    public const int MaxGroupsPerTeacher = 20;
    public const int MaxMembersPerGroup = 200;
    public const int MaxAssignmentsPerGroup = 300;
    public const int MaxGroupsPerStudent = 20;

    // Chalkashadigan belgilarsiz: 0/O, 1/I/L yo'q — kodni og'zaki aytish oson.
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int CodeLength = 6;

    public static string NewJoinCode(Random rng) =>
        new(Enumerable.Range(0, CodeLength).Select(_ => CodeAlphabet[rng.Next(CodeAlphabet.Length)]).ToArray());

    /// <summary>Foydalanuvchi kiritgan kod: katta harf, bo'shliq/tire olib tashlanadi; yaroqsiz bo'lsa null.</summary>
    public static string? NormalizeCode(string? code)
    {
        var c = new string((code ?? "").ToUpperInvariant().Where(ch => !char.IsWhiteSpace(ch) && ch != '-').ToArray());
        return c.Length == CodeLength && c.All(CodeAlphabet.Contains) ? c : null;
    }

    public static readonly string[] PracticeKinds = ["speaking", "writing", "reading", "listening"];
    public static readonly string[] MockModules = ["speaking", "writing", "listening", "reading"];

    /// <summary>Vazifa turi to'g'rimi (CEFR'da hozircha faqat Speaking va Writing bor).</summary>
    public static bool IsValidKind(string? kind)
    {
        if (kind is null) return false;
        if (kind == "review") return true;
        var p = kind.Split(':');
        return p switch
        {
            ["practice", var k] => PracticeKinds.Contains(k),
            ["mock", "ielts", var m] => MockModules.Contains(m),
            ["mock", "cefr", var m] => MockModules.Contains(m),
            _ => false,
        };
    }

    public static string? CleanName(string? name)
    {
        var n = (name ?? "").Trim();
        return n.Length is >= 2 and <= 80 ? n : null;
    }

    private static ActivityType? PracticeType(string k) => k switch
    {
        "speaking" => ActivityType.Speaking,
        "writing" => ActivityType.Writing,
        "reading" => ActivityType.Reading,
        "listening" => ActivityType.Listening,
        _ => null,
    };

    /// <summary>
    /// Vazifa holati: vazifa berilgandan KEYINGI birinchi mos urinish.
    /// "review" — shu vaqtdan beri kamida Target marta takrorlash.
    /// Muddat o'tgach bajarilgan bo'lsa — "kechikkan".
    /// </summary>
    public static AssignmentStatus StatusFor(Assignment a, IEnumerable<ActivityRef> activities, IEnumerable<DateTime> reviews)
    {
        if (a.Kind == "review")
        {
            var target = Math.Max(1, a.Target ?? 1);
            var after = reviews.Where(r => r >= a.CreatedAtUtc).OrderBy(r => r).ToList();
            if (after.Count < target) return new(false, false, null, null, null, after.Count, target);
            var doneAt = after[target - 1];
            return new(true, a.DueAtUtc is DateTime due && doneAt > due, doneAt, null, null, after.Count, target);
        }

        var p = a.Kind.Split(':');
        Func<ActivityRef, bool> match = p switch
        {
            ["practice", var k] when PracticeType(k) is ActivityType t => x => x.Type == t,
            ["mock", var exam, var module] => x => x.Type == ActivityType.MockExam && x.Exam == exam && x.Module == module,
            _ => _ => false,
        };
        var hit = activities.Where(x => x.CreatedAtUtc >= a.CreatedAtUtc && match(x)).OrderBy(x => x.CreatedAtUtc).FirstOrDefault();
        if (hit is null) return new(false, false, null, null, null, null, null);
        return new(true, a.DueAtUtc is DateTime d && hit.CreatedAtUtc > d, hit.CreatedAtUtc, hit.Id, hit.Score, null, null);
    }

    /// <summary>
    /// Natijaning qisqa ko'rinishi jadval uchun: IELTS band ("6.5"), CEFR
    /// ("55/75 · B2"), test ("3/4") yoki mezonlar o'rtachasi ("78/100").
    /// </summary>
    public static string? ScoreText(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var r = doc.RootElement;
            if (r.ValueKind != JsonValueKind.Object) return null;
            if (r.TryGetProperty("overall", out var o) && o.TryGetDecimal(out var overall))
            {
                return r.TryGetProperty("level", out var lv) && lv.ValueKind == JsonValueKind.String
                    ? $"{overall.ToString("0", CultureInfo.InvariantCulture)}/75 · {lv.GetString()}"
                    : overall.ToString("0.0", CultureInfo.InvariantCulture);
            }
            if (r.TryGetProperty("score", out var s) && s.TryGetInt32(out var score) && r.TryGetProperty("total", out var t) && t.TryGetInt32(out var total))
            {
                return $"{score}/{total}";
            }
            var scores = r.EnumerateObject()
                .Where(p => p.Value.ValueKind == JsonValueKind.Object && p.Value.TryGetProperty("score", out var v) && v.ValueKind == JsonValueKind.Number)
                .Select(p => p.Value.GetProperty("score").GetDouble())
                .ToList();
            return scores.Count > 0 ? $"{Math.Round(scores.Average()).ToString(CultureInfo.InvariantCulture)}/100" : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>O'qituvchiga ko'rinadigan ism: taxallus yoki emailning "@" gacha qismi.</summary>
    public static string DisplayNameOf(string? displayName, string email) =>
        !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : email.Split('@')[0];
}
