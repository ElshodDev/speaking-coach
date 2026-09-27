using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// O'qituvchi yangi vazifa berganda — Telegram'ni ulagan o'quvchilarga
/// xabar ("▶️ Bajarish" tugmasi bilan). Bot o'chiq bo'lsa — hech narsa
/// qilmaydi. Telegram cheklovi (sekundiga ~30 xabar) oshmasligi uchun
/// xabarlar orasida qisqa tanaffus.
/// </summary>
public class AssignmentNotifier(AppDbContext db, ITelegramApi api, TelegramOptions options, ILogger<AssignmentNotifier> logger)
{
    public async Task<int> NotifyAsync(Guid assignmentId, CancellationToken ct = default)
    {
        if (!options.Enabled) return 0;
        var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == assignmentId, ct);
        if (a is null) return 0;
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == a.GroupId, ct);
        if (group is null) return 0;
        var teacher = await db.Users.Where(u => u.Id == group.TeacherId).Select(u => new { u.DisplayName, u.Email }).FirstOrDefaultAsync(ct);
        var teacherName = GroupLogic.DisplayNameOf(teacher?.DisplayName, teacher?.Email ?? "");

        var memberIds = await db.GroupMembers.Where(m => m.GroupId == a.GroupId).Select(m => m.UserId).ToListAsync(ct);
        var accounts = await db.TelegramAccounts.Where(t => memberIds.Contains(t.UserId)).ToListAsync(ct);
        var sent = 0;
        foreach (var acc in accounts)
        {
            try
            {
                var html = BotToday.NewTaskMessage(acc.Lang, teacherName, group.Name, a.Kind, a.Target, a.Instructions, a.DueAtUtc, acc.TzOffsetMinutes);
                var route = GroupLogic.RouteFor(a.Kind);
                await api.SendMessageAsync(acc.ChatId, html, new[]
                {
                    new[] { a.Kind == "review"
                        ? new TgButton(Texts.Get(acc.Lang, "bot.task_do"), BotLogic.Encode(new BotCallback.StartReview()))
                        : options.SiteButton(Texts.Get(acc.Lang, "bot.task_do"), route) },
                }, ct: ct);
                sent++;
                await Task.Delay(50, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Yangi vazifa xabari yuborilmadi: chat {Chat}", acc.ChatId);
            }
        }
        if (sent > 0) logger.LogInformation("Yangi vazifa {Id}: {Sent} ta o'quvchiga Telegram xabari", a.Id, sent);
        return sent;
    }
}
