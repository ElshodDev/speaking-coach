using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

public record LoadedTalk(PendingExercise Row, TalkState State);

/// <summary>
/// AI suhbat holati: suhbat tarixi serverda (PendingExercises, tur — Conversation) saqlanadi — brauzer uni o'zgartira olmaydi.
/// Ochish, yuklash (egasi tekshiriladi), navbatni saqlash va yakunlash (xulosa + kartalar bitta tranzaksiyada).
/// AI chaqiruvi, kunlik navbat limiti va HTTP — Api'dagi TalkEndpoints'da.
/// </summary>
public class TalkSessions(IAppDbContext db, ReviewService reviews)
{
    /// <summary>Javob berilmay tashlab ketilgan eski suhbatlarni tozalash.</summary>
    public async Task CleanStaleAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        await db.PendingExercises.Where(p => p.CreatedAtUtc < cutoff).ExecuteDeleteAsync();
    }

    /// <summary>Suhbatni ochadi; suhbat id'sini qaytaradi.</summary>
    public async Task<Guid> StartAsync(TalkState state)
    {
        var row = new PendingExercise
        {
            Id = Guid.NewGuid(),
            Type = ActivityType.Conversation,
            Payload = JsonSerializer.Serialize(state, Talk.Json),
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.PendingExercises.Add(row);
        await db.SaveChangesAsync();
        return row.Id;
    }

    /// <summary>Suhbatni yuklaydi; yo'q, buzilgan yoki boshqa foydalanuvchiniki bo'lsa — null.</summary>
    public async Task<LoadedTalk?> LoadAsync(Guid id, Guid userId)
    {
        var row = await db.PendingExercises.FirstOrDefaultAsync(p => p.Id == id && p.Type == ActivityType.Conversation);
        if (row is null) return null;
        TalkState? state;
        try
        {
            state = JsonSerializer.Deserialize<TalkState>(row.Payload, Talk.Json);
        }
        catch (JsonException)
        {
            return null;
        }
        return state is null || state.UserId != userId ? null : new LoadedTalk(row, state);
    }

    /// <summary>Yangi navbatdan keyingi holatni saqlaydi.</summary>
    public async Task SaveTurnAsync(PendingExercise row, TalkState state)
    {
        row.Payload = JsonSerializer.Serialize(state, Talk.Json);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Suhbatni yakunlaydi: xulosa tarixga yoziladi, vaqtincha holat o'chadi, tuzatishlar kartalarga aylanadi — hammasi bitta SaveChanges'da.
    /// Ikki marta yakunlansa (parallel so'rov) — ikkinchisi null qaytaradi (409).
    /// </summary>
    public async Task<(Guid ActivityId, int NewCards)?> FinishAsync(Guid userId, LoadedTalk talk, TalkScenario scenario, TalkSummary summary,
        IReadOnlyList<CorrectionItem> corrections, FluencyStats? metrics)
    {
        var activityId = Guid.NewGuid();
        var full = summary with { TopCorrections = corrections.ToList() };
        db.Activities.Add(new Activity
        {
            Id = activityId,
            Type = ActivityType.Conversation,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            PromptData = JsonSerializer.Serialize(new { scenario = scenario.Id, level = talk.State.Level, lines = talk.State.Lines }, Talk.Json),
            ResponseData = metrics is null
                ? JsonSerializer.Serialize(full, Talk.Json)
                : FluencyMetrics.WithMetrics(full, metrics, Talk.Json),
        });
        db.PendingExercises.Remove(talk.Row);
        // Kartalar suhbat yozuvi bilan bitta SaveChanges'da saqlanadi: AddCardsAsync faqat kontekstga qo'shadi, o'zi saqlamaydi.
        var newCards = await reviews.AddCardsAsync(userId, ActivityType.Conversation, ReviewCardFactory.FromCorrections(corrections));
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }
        return (activityId, newCards);
    }
}
