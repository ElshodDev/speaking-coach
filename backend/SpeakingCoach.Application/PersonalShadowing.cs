using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Application.Mock;
namespace SpeakingCoach.Application;

/// <summary>
/// Shaxsiy podcast: foydalanuvchi YouTube videoni botda o'ziga shadowing darsi qilib qo'shadi (faqat o'zi ko'radi).
/// Dars <c>MockTests</c> jadvalida, holat "personal", muallif — CreatedByUserId (sxema o'zgarmaydi).
/// Gemini xarajati uchun kunlik limit (Ai:PodcastsPerDay, standart 3; admin — cheksiz) va saqlanadigan darslar soni chegarasi.
/// </summary>
public class PersonalShadowingService(IAppDbContext db, AiDailyCaps caps, AiQuotaOptions limits, AdminOptions admins)
{
    /// <summary>Bitta foydalanuvchida ko'pi bilan shuncha shaxsiy dars (bazaning cheksiz o'smasligi uchun).</summary>
    public const int MaxStored = 30;

    public static readonly AuthorKind Kind = new(ShadowingService.Exam, ShadowingRules.YouTube, "");

    public int PerDay => limits.Podcasts;

    /// <summary>Kunlik urinishdan bitta joy (false — bugungi limit tugagan). Admin — cheksiz.</summary>
    public bool TryTakeAttempt(User user) =>
        admins.IsAdmin(user) || caps.TryTake(AiDailyCaps.PodcastKey(user.Id), AiQuotaService.Today(DateTime.UtcNow), limits.Podcasts);

    /// <summary>Gemini'ga yetib bormadi yoki dars tuzilmadi — urinish qaytariladi.</summary>
    public void RefundAttempt(User user)
    {
        if (!admins.IsAdmin(user)) caps.Refund(AiDailyCaps.PodcastKey(user.Id), AiQuotaService.Today(DateTime.UtcNow));
    }

    public Task<int> CountAsync(Guid userId, CancellationToken ct = default) =>
        db.MockTests.CountAsync(t => t.Exam == ShadowingService.Exam && t.Status == MockTestStatus.Personal && t.CreatedByUserId == userId, ct);

    public async Task<List<(Guid Id, string Title, int Lines)>> ListAsync(Guid userId, int take = 8, CancellationToken ct = default)
    {
        var rows = await db.MockTests
            .Where(t => t.Exam == ShadowingService.Exam && t.Status == MockTestStatus.Personal && t.CreatedByUserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc).Take(take)
            .Select(t => new { t.Id, t.Title, t.Payload })
            .ToListAsync(ct);
        return [.. rows.Select(r => (r.Id, r.Title ?? "", LineCount(r.Payload)))];
    }

    private static int LineCount(string payload)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            return doc.RootElement.TryGetProperty("lines", out var l) && l.ValueKind == System.Text.Json.JsonValueKind.Array ? l.GetArrayLength() : 0;
        }
        catch (System.Text.Json.JsonException)
        {
            return 0;
        }
    }

    /// <summary>Tayyor dars shaxsiy qilib saqlanadi (admin ko'rib chiqishiga tushmaydi).</summary>
    public async Task<MockTest> SaveAsync(Guid userId, AuthoredTest test, CancellationToken ct = default)
    {
        var row = new MockTest
        {
            Id = Guid.NewGuid(),
            Exam = Kind.Exam,
            Module = Kind.Module,
            Variant = Kind.Variant,
            Payload = test.Payload,
            Title = test.Title.Length > 200 ? test.Title[..200] : test.Title,
            Status = MockTestStatus.Personal,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.MockTests.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    /// <summary>Faqat o'zining shaxsiy darsini o'chiradi (yumshoq emas: boshqa hech kim ko'rmaydi). true — o'chirildi.</summary>
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var n = await db.MockTests
            .Where(t => t.Id == id && t.Exam == ShadowingService.Exam && t.Status == MockTestStatus.Personal && t.CreatedByUserId == userId)
            .ExecuteDeleteAsync(ct);
        return n > 0;
    }
}
