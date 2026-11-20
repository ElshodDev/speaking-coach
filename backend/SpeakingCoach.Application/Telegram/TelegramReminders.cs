using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application.Telegram;

public record ReminderRun(int Checked, int Sent, int SkippedGoalDone, int Disabled);

/// <summary>
/// Cron so'rovi eslatmalarni kutmaydi: ish fonda (alohida DI scope'da) bajariladi, javob — darhol 202.
/// Foydalanuvchilar ko'p bo'lsa, ketma-ket yuborish GitHub Actions'ning 90 soniyalik
/// muddatidan oshib ketardi. Bir vaqtda faqat bitta yurish (takroriy cron — e'tiborsiz).
/// </summary>
public sealed class ReminderJob(IServiceScopeFactory scopes, ILogger<ReminderJob> logger)
{
    private int _running;

    public bool IsRunning => Volatile.Read(ref _running) == 1;

    /// <summary>Fonda boshlaydi; allaqachon ishlayotgan bo'lsa — false.</summary>
    public bool TryStart(DateTime nowUtc)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return false;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopes.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<TelegramReminders>();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var run = await reminders.RunAsync(nowUtc, CancellationToken.None);
                var cleaned = await Maintenance.CleanupAsync(db, DateTime.UtcNow);
                logger.LogInformation("Eslatmalar: tekshirildi {Checked}, yuborildi {Sent}, maqsad bajarilgan {Done}, o'chirildi {Disabled}; tozalandi {Cleaned}",
                    run.Checked, run.Sent, run.SkippedGoalDone, run.Disabled, cleaned);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Eslatmalar yurishi xato bilan tugadi");
            }
            finally
            {
                Volatile.Write(ref _running, 0);
            }
        });
        return true;
    }
}

/// <summary>
/// Kunlik eslatmalar. Render'ning bepul serveri uxlab qoladi va o'zi
/// taymer bilan ishlay olmaydi — shuning uchun GitHub Actions har soatda
/// /api/telegram/cron'ni chaqiradi (serverni uyg'otadi), bu yerda esa
/// vaqti kelgan eslatmalar yuboriladi. Qoida: kuniga ko'pi bilan BITTA
/// xabar; bugungi maqsad bajarilgan bo'lsa — umuman yozmaymiz.
/// </summary>
public class TelegramReminders
{
    private readonly IAppDbContext _db;
    private readonly ITelegramApi _api;
    private readonly ReviewService _reviews;
    private readonly TodayService _today;
    private readonly ILogger<TelegramReminders> _logger;

    public TelegramReminders(IAppDbContext db, ITelegramApi api, ReviewService reviews, TodayService today, ILogger<TelegramReminders> logger)
    {
        _today = today;
        _db = db;
        _api = api;
        _reviews = reviews;
        _logger = logger;
    }

    public async Task<ReminderRun> RunAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var accounts = await _db.TelegramAccounts.AsNoTracking().Where(a => a.ReminderHour != null).ToListAsync(ct);
        int sent = 0, goalDone = 0, disabled = 0, checkedCount = 0;

        // Boshlangan ish oxirigacha bajariladi (cron so'rovi uzilsa ham) —
        // aks holda yarim yo'lda to'xtab, keyingi chaqiruvda takror yuborilardi.
        ct = CancellationToken.None;

        foreach (var a in accounts.Where(a => BotLogic.ShouldRemind(a.ReminderHour, a.LastReminderDate, nowUtc, a.TzOffsetMinutes)))
        {
            // Avval "bugun yuborildi" deb belgilaymiz (atomar): ikki cron parallel
            // ishlasa ham (curl --retry) bitta foydalanuvchiga faqat bittasi yozadi.
            var today = (DateOnly?)BotLogic.LocalToday(nowUtc, a.TzOffsetMinutes);
            var claimed = await _db.TelegramAccounts
                .Where(x => x.ChatId == a.ChatId && x.ReminderHour != null && (x.LastReminderDate == null || x.LastReminderDate != today))
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.LastReminderDate, today), ct);
            if (claimed == 0) continue;

            checkedCount++;
            try
            {
                var s = await _reviews.GetStatsAsync(a.UserId, a.TzOffsetMinutes);
                var tasks = await _today.PendingTasksAsync(a.UserId);
                if (s.ReviewedToday >= s.DailyGoal && tasks.Count == 0)
                {
                    goalDone++;
                    continue; // bugun allaqachon shug'ullangan va vazifa yo'q — bezovta qilmaymiz
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == a.UserId, ct);
                int? daysToExam = user?.ExamDate is DateOnly exam && exam >= BotLogic.LocalToday(nowUtc, a.TzOffsetMinutes)
                    ? exam.DayNumber - BotLogic.LocalToday(nowUtc, a.TzOffsetMinutes).DayNumber
                    : null;
                var lines = new List<string>
                {
                    s.StreakDays > 0 ? Texts.Get(a.Lang, "bot.reminder_streak", s.StreakDays) : Texts.Get(a.Lang, "bot.reminder_start"),
                };
                if (s.ReviewedToday < s.DailyGoal) lines.Add(s.Due > 0 ? Texts.Get(a.Lang, "bot.reminder_due", s.Due) : Texts.Get(a.Lang, "bot.reminder_nodue"));
                lines.AddRange(BotToday.ReminderExtra(a.Lang, tasks, user?.Goal, daysToExam, nowUtc));
                var buttons = new List<TgButton[]>();
                if (s.Due > 0 && s.ReviewedToday < s.DailyGoal)
                    buttons.Add(new[] { new TgButton(Texts.Get(a.Lang, "bot.start_button"), BotLogic.Encode(new BotCallback.StartReview())) });
                buttons.Add(new[] { new TgButton(Texts.Get(a.Lang, "bot.today_button"), BotLogic.Encode(new BotCallback.ShowToday())) });
                await _api.SendMessageAsync(a.ChatId, string.Join("\n", lines), buttons, ct: ct);
                sent++;
            }
            catch (TelegramApiException ex) when (ex.ChatGone)
            {
                // Foydalanuvchi botni bloklagan — eslatmalarni o'chiramiz, boshqa urinmaymiz.
                await _db.TelegramAccounts.Where(x => x.ChatId == a.ChatId)
                    .ExecuteUpdateAsync(set => set.SetProperty(x => x.ReminderHour, (int?)null), ct);
                disabled++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Eslatma yuborilmadi: chat {Chat}", a.ChatId);
            }
        }

        return new ReminderRun(checkedCount, sent, goalDone, disabled);
    }
}
