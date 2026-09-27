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
    public static readonly BuiltInMock[] All =
    [
        new(Guid.Parse("5c0e1a00-0000-4000-8000-000000000101"), "ielts", "listening", "", "Built-in IELTS Listening 1", BuiltInIelts.Listening1),
        new(Guid.Parse("5c0e1a00-0000-4000-8000-000000000102"), "ielts", "reading", IeltsBank.Academic, "Built-in IELTS Reading (Academic) 1", BuiltInIelts.ReadingAcademic1),
        new(Guid.Parse("5c0e1a00-0000-4000-8000-000000000201"), "cefr", "listening", "", "Built-in CEFR Listening 1", BuiltInCefr.Listening1),
        new(Guid.Parse("5c0e1a00-0000-4000-8000-000000000202"), "cefr", "reading", "", "Built-in CEFR Reading 1", BuiltInCefr.Reading1),
    ];

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
