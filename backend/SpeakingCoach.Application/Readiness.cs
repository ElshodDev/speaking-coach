using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Application;

/// <summary>Bitta dalil: qaysi ko'nikma, qayerdan (mock / Part 2 / mashq), imtihon shkalasidagi qiymati va vaqti.</summary>
public record ReadinessEvidence(string Skill, string Source, decimal Value, DateTime AtUtc);

public record SkillReadiness(
    [property: JsonPropertyName("skill")] string Skill,
    [property: JsonPropertyName("estimate")] decimal? Estimate,
    [property: JsonPropertyName("low")] decimal? Low,
    [property: JsonPropertyName("high")] decimal? High,
    [property: JsonPropertyName("confidence")] string Confidence,
    [property: JsonPropertyName("mocks")] int Mocks,
    [property: JsonPropertyName("practice")] int Practice,
    [property: JsonPropertyName("lastAtUtc")] DateTime? LastAtUtc);

/// <summary>
/// Imtihonga tayyorlik bahosi. IELTS — band (0–9, 0.5 qadam); CEFR Multilevel — ball (0–75) va daraja.
/// Hammasi taxminiy: so'nggi 90 kundagi mock va mashq natijalaridan, yangilariga ko'proq og'irlik bilan.
/// </summary>
public record ReadinessReport(
    [property: JsonPropertyName("exam")] string? Exam,
    [property: JsonPropertyName("max")] decimal Max,
    [property: JsonPropertyName("overall")] decimal? Overall,
    [property: JsonPropertyName("overallLow")] decimal? OverallLow,
    [property: JsonPropertyName("overallHigh")] decimal? OverallHigh,
    [property: JsonPropertyName("overallLevel")] string? OverallLevel,
    [property: JsonPropertyName("target")] string? Target,
    [property: JsonPropertyName("targetValue")] decimal? TargetValue,
    [property: JsonPropertyName("gap")] decimal? Gap,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("daysToExam")] int? DaysToExam,
    [property: JsonPropertyName("focusSkill")] string? FocusSkill,
    [property: JsonPropertyName("focusReason")] string? FocusReason,
    [property: JsonPropertyName("skills")] List<SkillReadiness> Skills,
    // Qo'shimcha (ixtiyoriy): Features:DayOneDiagnostic yoqiq bo'lsa true — frontend "boshlang'ich test" kartasini ko'rsatadi.
    [property: JsonPropertyName("diagnosticEnabled")] bool DiagnosticEnabled = false);

/// <summary>Hisoblash qoidalari — toza funksiyalar (unit test qilinadi).</summary>
public static class ReadinessCalculator
{
    public static readonly string[] Skills = ["listening", "reading", "writing", "speaking"];

    /// <summary>Dalillar shuncha kundan eski bo'lmasin; og'irlik har 30 kunda ikki baravar kamayadi.</summary>
    public const int WindowDays = 90;
    public const double HalfLifeDays = 30;

    /// <summary>Manba og'irligi: to'liq mock bo'limi — eng ishonchli; Part 2 — o'rtacha; oddiy mashq — taxminiy.</summary>
    public static decimal SourceWeight(string source) => source switch
    {
        "mock" => 1m,
        "part2" => 0.6m,
        _ => 0.35m,
    };

    // ---- Shkalalar ----

    /// <summary>Mashq bali (0–100, mezonlar o'rtachasi) → IELTS band (taxminiy, chiziqli): 50 → 5.5, 70 → 6.9, 100 → 9.</summary>
    public static decimal PracticeToBand(decimal score) => Math.Clamp(2m + Math.Clamp(score, 0, 100) * 0.07m, 0m, 9m);

    /// <summary>
    /// IELTS band → CEFR Multilevel bali (0–75), keng tarqalgan moslik bo'yicha bo'lakli chiziq:
    /// 4.0 → 38 (B1), 5.5 → 51 (B2), 7.0 → 65 (C1), 9.0 → 75.
    /// </summary>
    public static decimal BandToCefr(decimal band)
    {
        (decimal B, decimal S)[] p = [(0m, 0m), (4m, 38m), (5.5m, 51m), (7m, 65m), (9m, 75m)];
        var b = Math.Clamp(band, 0m, 9m);
        for (var i = 1; i < p.Length; i++)
            if (b <= p[i].B)
                return p[i - 1].S + (b - p[i - 1].B) / (p[i].B - p[i - 1].B) * (p[i].S - p[i - 1].S);
        return 75m;
    }

    public static decimal Max(string exam) => exam == "cefr" ? 75m : 9m;

    public static decimal Round(string exam, decimal v) =>
        exam == "cefr" ? Math.Clamp(Math.Round(v, MidpointRounding.AwayFromZero), 0, 75) : IeltsBand.RoundHalfBand(v);

    /// <summary>Maqsad → shkala qiymati: IELTS "6.5" → 6.5; CEFR "B2" → 51 (daraja chegarasi).</summary>
    public static decimal? TargetValue(string exam, string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;
        if (exam == "cefr") return target switch { "B1" => 38m, "B2" => 51m, "C1" => 65m, _ => null };
        return decimal.TryParse(target, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var t) && t is >= 0 and <= 9 ? t : null;
    }

    // ---- Hisob ----

    public static SkillReadiness ForSkill(string exam, string skill, IEnumerable<ReadinessEvidence> all, DateTime nowUtc)
    {
        var ev = all.Where(e => e.Skill == skill && e.AtUtc > nowUtc.AddDays(-WindowDays) && e.AtUtc <= nowUtc.AddMinutes(5)).ToList();
        if (ev.Count == 0) return new SkillReadiness(skill, null, null, null, "none", 0, 0, null);

        decimal sum = 0, weights = 0;
        foreach (var e in ev)
        {
            var age = Math.Max(0, (nowUtc - e.AtUtc).TotalDays);
            var w = SourceWeight(e.Source) * (decimal)Math.Pow(0.5, age / HalfLifeDays);
            sum += e.Value * w;
            weights += w;
        }
        var estimate = Round(exam, sum / weights);

        // Ishonch: dalillarning (eskirmagan) og'irliklari yig'indisi — 1 ta mock ≈ "o'rtacha", 2+ ≈ "yuqori".
        var strength = ev.Sum(e => SourceWeight(e.Source));
        var confidence = strength >= 2m ? "high" : strength >= 1m ? "medium" : "low";
        // Mock'siz baho — taxminiy (mashq bali bandga taxminan o'giriladi): ko'p mashq ham "yuqori ishonch" bermaydi.
        // Part 2 (IELTS shaklidagi baho) bo'lsa — ko'pi bilan "o'rtacha", faqat oddiy mashq bo'lsa — "past".
        if (!ev.Any(e => e.Source == "mock"))
            confidence = ev.Any(e => e.Source == "part2") && confidence != "low" ? "medium" : "low";
        var half = (exam, confidence) switch
        {
            ("cefr", "high") => 3m,
            ("cefr", "medium") => 5m,
            ("cefr", _) => 8m,
            (_, "high") => 0.5m,
            (_, "medium") => 0.5m,
            _ => 1m,
        };
        return new SkillReadiness(skill, estimate,
            Math.Max(0, estimate - half), Math.Min(Max(exam), estimate + half), confidence,
            ev.Count(e => e.Source == "mock"), ev.Count(e => e.Source != "mock"), ev.Max(e => e.AtUtc));
    }

    public static ReadinessReport Build(string? goal, string? target, DateOnly? examDate, DateOnly today,
        IEnumerable<ReadinessEvidence> evidence, DateTime nowUtc)
    {
        int? daysToExam = examDate is { } d && d >= today ? d.DayNumber - today.DayNumber : null;
        if (goal is not ("ielts" or "cefr"))
            return new ReadinessReport(null, 9m, null, null, null, null, null, null, null, "no_goal", daysToExam, null, null, []);

        var exam = goal;
        var list = evidence.ToList();
        var skills = Skills.Select(s => ForSkill(exam, s, list, nowUtc)).ToList();

        decimal? overall = null, low = null, high = null;
        if (skills.All(s => s.Estimate is not null))
        {
            overall = Round(exam, skills.Average(s => s.Estimate!.Value));
            low = Round(exam, skills.Average(s => s.Low!.Value));
            high = Round(exam, skills.Average(s => s.High!.Value));
        }

        var targetValue = TargetValue(exam, target);
        decimal? gap = overall is { } o && targetValue is { } t ? t - o : null;
        var status = overall is null ? "unknown"
            : gap is null ? "no_target"
            : gap <= 0 ? "on_track"
            : gap <= (exam == "cefr" ? 5m : 0.5m) ? "close"
            : "behind";

        // Fokus: avval ma'lumoti yo'q bo'lim (mock topshirish kerak), keyin — maqsaddan eng uzoq (eng zaif) ko'nikma.
        var missing = skills.FirstOrDefault(s => s.Estimate is null);
        var (focus, reason) = missing is not null
            ? (missing.Skill, "missing")
            : (skills.OrderBy(s => s.Estimate).ThenBy(s => s.Confidence == "high" ? 1 : 0).First().Skill, "weakest");

        return new ReadinessReport(exam, Max(exam), overall, low, high,
            exam == "cefr" && overall is { } oc ? CefrScale.Level((int)oc) : null,
            target, targetValue, gap, status, daysToExam, focus, reason, skills);
    }

    /// <summary>
    /// Speaking/Writing mashq natijasidan dalil: Part 2 — "estimatedBand"; oddiy mashq — mezonlar o'rtachasi (0–100).
    /// Shkalaga o'tkazilgan qiymat qaytariladi; o'qib bo'lmasa — null.
    /// </summary>
    public static ReadinessEvidence? FromPractice(string exam, ActivityType type, string responseJson, DateTime atUtc, bool useSpeakingBands = false)
    {
        var skill = type switch { ActivityType.Speaking => "speaking", ActivityType.Writing => "writing", _ => null };
        if (skill is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var r = doc.RootElement;
            if (r.ValueKind != JsonValueKind.Object) return null;
            if (r.TryGetProperty("estimatedBand", out var eb) && eb.TryGetDecimal(out var band))
                return new ReadinessEvidence(skill, "part2", exam == "cefr" ? BandToCefr(band) : Math.Clamp(band, 0, 9), atUtc);
            // Gapirish mashqining 4 mezonli bahosi (Features:SpeakingFourCriteria): modelning o'z band bahosi,
            // 0–100 ballni bandga chiziqli o'girishdan aniqroq.
            if (useSpeakingBands && type == ActivityType.Speaking && r.TryGetProperty("overallBand", out var ob) && ob.ValueKind == JsonValueKind.Number && ob.TryGetDecimal(out var overallBand) && overallBand is >= 0 and <= 9)
                return new ReadinessEvidence(skill, "practice", exam == "cefr" ? BandToCefr(overallBand) : overallBand, atUtc);
            string[] criteria = type == ActivityType.Writing
                ? ["taskAchievement", "coherenceCohesion", "grammar", "vocabulary"]
                : ["fluency", "grammar", "vocabulary"];
            var scores = criteria
                .Select(c => r.TryGetProperty(c, out var o) && o.ValueKind == JsonValueKind.Object && o.TryGetProperty("score", out var s) && s.TryGetDecimal(out var v) ? v : (decimal?)null)
                .ToList();
            if (scores.Any(s => s is null)) return null;
            var b2 = PracticeToBand(scores.Average(s => s!.Value));
            return new ReadinessEvidence(skill, "practice", exam == "cefr" ? BandToCefr(b2) : b2, atUtc);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// O'qish/Tinglash mashqidan dalil: faqat ilovaning TAYYOR (darajasi ma'lum) mashqlaridan — sarlavha bo'yicha
    /// kutubxonadan daraja topiladi; AI yozgan mashqning darajasi noma'lum — dalil yo'q (null).
    /// </summary>
    public static ReadinessEvidence? FromComprehension(string exam, ActivityType type, string promptJson, string responseJson, DateTime atUtc)
    {
        var skill = type switch { ActivityType.Reading => "reading", ActivityType.Listening => "listening", _ => null };
        if (skill is null) return null;
        try
        {
            using var prompt = JsonDocument.Parse(promptJson);
            if (prompt.RootElement.ValueKind != JsonValueKind.Object || !prompt.RootElement.TryGetProperty("title", out var t) || t.ValueKind != JsonValueKind.String) return null;
            var title = t.GetString();
            var level = PracticeLibrary.For(type).FirstOrDefault(x => x.Exercise.Title == title)?.Level;
            using var resp = JsonDocument.Parse(responseJson);
            var r = resp.RootElement;
            if (r.ValueKind != JsonValueKind.Object || !r.TryGetProperty("score", out var s) || !s.TryGetInt32(out var score) || !r.TryGetProperty("total", out var tt) || !tt.TryGetInt32(out var total)) return null;
            if (ComprehensionBand.Estimate(level, score, total) is not { } band) return null;
            return new ReadinessEvidence(skill, "practice", exam == "cefr" ? BandToCefr(band) : band, atUtc);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>Bazadan dalillarni yig'adi (yengil so'rovlar) va hisobotni tuzadi.</summary>
public class ReadinessService(IAppDbContext db, IMockRows mockRows, FeatureFlags? flags = null)
{
    /// <summary>Oxirgi shuncha O'qish/Tinglash mashqi hisobga olinadi (matnlar katta — cheklangan).</summary>
    public const int ComprehensionTake = 20;

    public async Task<ReadinessReport> ForUserAsync(User user, DateOnly today, DateTime nowUtc)
    {
        var exam = user.Goal is "ielts" or "cefr" ? user.Goal : null;
        var evidence = new List<ReadinessEvidence>();
        if (exam is not null)
        {
            var since = nowUtc.AddDays(-ReadinessCalculator.WindowDays);
            // Mock'lar: faqat kerakli maydonlar (overall, module) — SQL'ning o'zida jsonb'dan.
            foreach (var m in await mockRows.ForUserAsync(user.Id, 200))
            {
                if (m.CreatedAtUtc < since || m.Overall is null || (m.Exam ?? "ielts") != exam) continue;
                if (!ReadinessCalculator.Skills.Contains(m.Module)) continue;
                evidence.Add(new ReadinessEvidence(m.Module!, "mock", m.Overall.Value, m.CreatedAtUtc));
            }
            // Speaking/Writing mashqlari (Part 2 ham) — oxirgilari, cheklangan son.
            var practice = await db.Activities
                .Where(a => a.UserId == user.Id && a.CreatedAtUtc >= since && (a.Type == ActivityType.Speaking || a.Type == ActivityType.Writing))
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(60)
                .Select(a => new { a.Type, a.CreatedAtUtc, a.ResponseData })
                .ToListAsync();
            var useBands = flags?.SpeakingFourCriteria ?? false;
            evidence.AddRange(practice.Select(p => ReadinessCalculator.FromPractice(exam, p.Type, p.ResponseData, p.CreatedAtUtc, useBands)).OfType<ReadinessEvidence>());
            // Boshlang'ich baho (Features:DayOneDiagnostic): ilovaning tayyor O'qish/Tinglash mashqlari darajasi ma'lum —
            // ulardan past ishonchli taxminiy dalil olinadi (mock'siz ham tayyorlik birinchi kundan hisoblanadi).
            if (flags?.DayOneDiagnostic == true)
            {
                var comp = await db.Activities
                    .Where(a => a.UserId == user.Id && a.CreatedAtUtc >= since && (a.Type == ActivityType.Reading || a.Type == ActivityType.Listening))
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Take(ComprehensionTake)
                    .Select(a => new { a.Type, a.CreatedAtUtc, a.PromptData, a.ResponseData })
                    .ToListAsync();
                evidence.AddRange(comp.Select(c => ReadinessCalculator.FromComprehension(exam, c.Type, c.PromptData, c.ResponseData, c.CreatedAtUtc)).OfType<ReadinessEvidence>());
            }
        }
        var report = ReadinessCalculator.Build(user.Goal, user.TargetScore, user.ExamDate, today, evidence, nowUtc);
        return report with { DiagnosticEnabled = exam is not null && flags?.DayOneDiagnostic == true };
    }
}
