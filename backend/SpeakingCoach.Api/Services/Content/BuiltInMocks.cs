using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Services.Content;

public record BuiltInMock(Guid Id, string Exam, string Module, string Variant, string Title, object Content);

/// <summary>
/// Ilova bilan birga keladigan to'liq mock testlar (Listening/Reading) —
/// original, rasmiy formatga mos va validatorlardan o'tgan. Birinchi so'rovda
/// bankka (MockTests, published) qo'shiladi; qatorlar Id bo'yicha bir marta.
/// </summary>
public static class BuiltInMocks
{
    public const int TestsPerKind = 11;

    /// <summary>Barqaror Id: 1-testlar avvalgi Id'larini saqlaydi (bazada allaqachon bo'lishi mumkin).</summary>
    private static Guid Id(int kind, int n) => (kind, n) switch
    {
        (11, 1) => Guid.Parse("5c0e1a00-0000-4000-8000-000000000101"),
        (12, 1) => Guid.Parse("5c0e1a00-0000-4000-8000-000000000102"),
        (21, 1) => Guid.Parse("5c0e1a00-0000-4000-8000-000000000201"),
        (22, 1) => Guid.Parse("5c0e1a00-0000-4000-8000-000000000202"),
        _ => Guid.Parse($"5c0e1a00-0000-4000-8000-{kind:D4}{n:D8}"),
    };

    private static readonly ListeningTest[] IeltsListening =
    [
        BuiltInIelts.Listening1, BuiltInIelts.Listening2, BuiltInIelts.Listening3, BuiltInIelts.Listening4, BuiltInIelts.Listening5, BuiltInIelts.Listening6,
        BuiltInIelts.Listening7, BuiltInIelts.Listening8, BuiltInIelts.Listening9, BuiltInIelts.Listening10, BuiltInIelts.Listening11,
    ];

    private static readonly ReadingTest[] IeltsReading =
    [
        BuiltInIelts.ReadingAcademic1, BuiltInIelts.ReadingAcademic2, BuiltInIelts.ReadingAcademic3, BuiltInIelts.ReadingAcademic4, BuiltInIelts.ReadingAcademic5, BuiltInIelts.ReadingAcademic6,
        BuiltInIelts.ReadingAcademic7, BuiltInIelts.ReadingAcademic8, BuiltInIelts.ReadingAcademic9, BuiltInIelts.ReadingAcademic10, BuiltInIelts.ReadingAcademic11,
    ];

    private static readonly ListeningTest[] CefrListening =
    [
        BuiltInCefr.Listening1, BuiltInCefr.Listening2, BuiltInCefr.Listening3, BuiltInCefr.Listening4, BuiltInCefr.Listening5, BuiltInCefr.Listening6,
        BuiltInCefr.Listening7, BuiltInCefr.Listening8, BuiltInCefr.Listening9, BuiltInCefr.Listening10, BuiltInCefr.Listening11,
    ];

    private static readonly ReadingTest[] CefrReading =
    [
        BuiltInCefr.Reading1, BuiltInCefr.Reading2, BuiltInCefr.Reading3, BuiltInCefr.Reading4, BuiltInCefr.Reading5, BuiltInCefr.Reading6,
        BuiltInCefr.Reading7, BuiltInCefr.Reading8, BuiltInCefr.Reading9, BuiltInCefr.Reading10, BuiltInCefr.Reading11,
    ];

    /// <summary>Hali to'ldirilmagan (bo'sh) testlar bankka qo'shilmaydi.</summary>
    public static readonly BuiltInMock[] All =
    [
        .. IeltsListening.Select((t, i) => new BuiltInMock(Id(11, i + 1), "ielts", "listening", "", $"IELTS Listening — Test {i + 1}", t)).Where(m => ((ListeningTest)m.Content).Parts.Count > 0),
        .. IeltsReading.Select((t, i) => new BuiltInMock(Id(12, i + 1), "ielts", "reading", IeltsBank.Academic, $"IELTS Reading (Academic) — Test {i + 1}", t)).Where(m => ((ReadingTest)m.Content).Passages.Count > 0),
        .. CefrListening.Select((t, i) => new BuiltInMock(Id(21, i + 1), "cefr", "listening", "", $"CEFR Listening — Test {i + 1}", t)).Where(m => ((ListeningTest)m.Content).Parts.Count > 0),
        .. CefrReading.Select((t, i) => new BuiltInMock(Id(22, i + 1), "cefr", "reading", "", $"CEFR Reading — Test {i + 1}", t)).Where(m => ((ReadingTest)m.Content).Passages.Count > 0),
    ];

    /// <summary>Test raqami (1, 2, …) — ro'yxatda "Test 3" deb ko'rsatish uchun; tayyor test bo'lmasa null.</summary>
    public static int? NumberOf(Guid id)
    {
        var m = All.FirstOrDefault(x => x.Id == id);
        if (m is null) return null;
        var digits = new string(m.Title.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return int.TryParse(digits, out var n) ? n : null;
    }

    public static bool IsBuiltIn(Guid id) => All.Any(x => x.Id == id);

    private static int _seeded;

    /// <summary>Bankda yo'q bo'lsa qo'shadi (jarayon davomida bir marta; xato bo'lsa keyingi so'rovda qayta urinadi).</summary>
    /// <remarks>Alohida DbContext'da — xato bo'lsa, so'rovning o'z konteksti "ifloslanmasin".</remarks>
    public static async Task EnsureSeededAsync(IServiceScopeFactory scopes, ILogger logger, CancellationToken ct = default)
    {
        if (Volatile.Read(ref _seeded) == 1) return;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            var ids = All.Select(x => x.Id).ToList();
            var existing = await db.MockTests.Where(t => ids.Contains(t.Id)).Select(t => t.Id).ToListAsync(ct);
            var missing = All.Where(x => !existing.Contains(x.Id)).ToList();
            foreach (var m in missing)
            {
                db.MockTests.Add(new MockTest
                {
                    Id = m.Id,
                    Exam = m.Exam,
                    Module = m.Module,
                    Variant = m.Variant,
                    Payload = JsonSerializer.Serialize(m.Content, m.Content.GetType(), GeminiMockGenerator.Web),
                    CreatedByUserId = null,
                    // Tayyor testlar bankda birinchi turadi (eng eski sana).
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Status = MockTestStatus.Published,
                    Title = m.Title,
                });
            }
            if (missing.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Tayyor mock testlar bankka qo'shildi: {Count}", missing.Count);
            }
            Volatile.Write(ref _seeded, 1);
        }
        catch (DbUpdateException ex)
        {
            // Boshqa so'rov shu paytda qo'shib ulgurgan — keyingi safar tekshiriladi.
            logger.LogWarning(ex, "Tayyor testlarni qo'shib bo'lmadi");
        }
    }
}
