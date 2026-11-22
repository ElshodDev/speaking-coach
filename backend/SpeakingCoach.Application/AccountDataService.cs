using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

/// <summary>
/// Foydalanuvchining o'z ma'lumotlari ustidan nazorati: hammasini JSON qilib yuklab olish va hisobni butunlay o'chirish.
/// Maxfiylik siyosatidagi va'daning texnik tomoni. HTTP va diskdagi fayllar — Api qatlamida.
/// </summary>
public class AccountDataService(IAppDbContext db)
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    /// <summary>Barcha ma'lumotlar bitta JSON faylda. jsonb ustunlar (mashq matni va baholar) ichma-ich JSON sifatida — matn qatori sifatida emas.</summary>
    public async Task<byte[]> ExportJsonAsync(User user)
    {
        var activities = await db.Activities
            .Where(a => a.UserId == user.Id)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => new { a.Id, a.Type, a.CreatedAtUtc, a.PromptData, a.ResponseData })
            .ToListAsync();
        var cards = await db.ReviewCards
            .Where(c => c.UserId == user.Id)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync();
        var logs = await db.ReviewLogs
            .Where(l => l.UserId == user.Id)
            .OrderBy(l => l.ReviewedAtUtc)
            .Select(l => new { l.CardId, l.Grade, l.ReviewedAtUtc })
            .ToListAsync();

        var taught = await db.Groups.Where(g => g.TeacherId == user.Id).Select(g => new { g.Id, g.Name, g.CreatedAtUtc }).ToListAsync();
        var taughtIds = taught.Select(g => g.Id).ToList();
        var assignments = await db.Assignments
            .Where(a => taughtIds.Contains(a.GroupId))
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => new { a.Id, a.GroupId, a.Kind, a.Target, a.Instructions, a.DueAtUtc, a.CreatedAtUtc })
            .ToListAsync();
        var memberOf = await db.GroupMembers.Where(m => m.UserId == user.Id).Select(m => new { m.GroupId, m.JoinedAtUtc }).ToListAsync();
        var feedback = await db.Feedbacks
            .Where(f => f.UserId == user.Id)
            .OrderBy(f => f.CreatedAtUtc)
            .Select(f => new { f.Id, f.Kind, f.Message, f.Page, f.Lang, f.CreatedAtUtc, f.Ip })
            .ToListAsync();
        var telegram = await db.TelegramAccounts
            .Where(t => t.UserId == user.Id)
            .Select(t => new { t.ChatId, t.Username, t.Lang, t.ReminderHour, t.TzOffsetMinutes, t.LinkedAtUtc, t.LastSeenAtUtc, t.LastReminderDate })
            .ToListAsync();
        var sessions = await db.Sessions
            .Where(x => x.UserId == user.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new { x.CreatedAtUtc, x.ExpiresAtUtc })
            .ToListAsync();
        var aiUsage = await db.AiUsages
            .Where(u => u.UserId == user.Id)
            .OrderBy(u => u.Day)
            .Select(u => new { u.Day, u.Exercises, u.Words, u.Shadowing })
            .ToListAsync();
        var authored = await db.MockTests
            .Where(m => m.CreatedByUserId == user.Id)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new { m.Id, m.Exam, m.Module, m.Variant, m.Title, m.Status, m.CreatedAtUtc, m.Payload })
            .ToListAsync();

        var export = new
        {
            exportedAtUtc = DateTime.UtcNow,
            account = new
            {
                user.Email,
                user.CreatedAtUtc,
                user.EmailVerifiedAtUtc,
                user.DisplayName,
                user.Level,
                user.ShowOnLeaderboard,
                user.Goal,
                user.TargetScore,
                user.ExamDate,
                user.DailyMinutes,
                user.OnboardedAtUtc,
                user.SignupMethod,
                hasPassword = !string.IsNullOrEmpty(user.PasswordHash),
            },
            activities = activities.Select(a => new
            {
                a.Id,
                type = a.Type.ToString(),
                a.CreatedAtUtc,
                prompt = ParseJson(a.PromptData),
                result = ParseJson(a.ResponseData),
            }),
            reviewCards = cards.Select(c => new
            {
                c.Id,
                kind = c.Kind.ToString(),
                source = c.Source?.ToString(),
                c.Front,
                c.Back,
                c.Note,
                c.CreatedAtUtc,
                c.DueAtUtc,
                c.IntervalDays,
                c.Repetitions,
                c.Lapses,
            }),
            reviewLog = logs,
            groupsTaught = taught,
            assignmentsCreated = assignments,
            groupMemberships = memberOf,
            mockTestsAuthored = authored.Select(m => new { m.Id, m.Exam, m.Module, m.Variant, m.Title, m.Status, m.CreatedAtUtc, content = ParseJson(m.Payload) }),
            feedback,
            telegram,
            // Faqat vaqtlar — token xeshlari (sir) eksportga kirmaydi.
            sessions,
            aiUsage,
        };

        return JsonSerializer.SerializeToUtf8Bytes(export, Pretty);
    }

    /// <summary>
    /// Hisobni va unga bog'liq hamma narsani o'chiradi (mashqlar, kartalar, sessiyalar, kodlar, limitlar — cascade bilan).
    /// Diskdagi ovoz yozuvlarini o'chirish uchun Speaking mashq id'lari qaytariladi.
    /// </summary>
    public async Task<List<Guid>> DeleteAccountAsync(User user)
    {
        var audioIds = await db.Activities
            .Where(a => a.UserId == user.Id && a.Type == ActivityType.Speaking)
            .Select(a => a.Id)
            .ToListAsync();

        // Shaxsiy (faqat o'zi ko'radigan) darslar hisob bilan birga o'chadi; chop etilganlar bankda qoladi.
        await db.MockTests.Where(m => m.CreatedByUserId == user.Id && m.Status == MockTestStatus.Personal).ExecuteDeleteAsync();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return audioIds;
    }

    private static JsonElement? ParseJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
