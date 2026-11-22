using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SpeakingCoach.Application.Abstractions;

/// <summary>
/// Ma'lumotlar bazasi Application qatlamiga shu interfeys orqali ko'rinadi.
/// Amalga oshiruvchi — Infrastructure'dagi AppDbContext (Postgres, Npgsql).
/// Servislar konkret provayderni bilmaydi: LINQ so'rovlari va SaveChanges yetarli.
/// </summary>
public interface IAppDbContext
{
    DbSet<Activity> Activities { get; }
    DbSet<User> Users { get; }
    DbSet<Session> Sessions { get; }
    DbSet<PendingExercise> PendingExercises { get; }
    DbSet<ReviewCard> ReviewCards { get; }
    DbSet<ReviewLog> ReviewLogs { get; }
    DbSet<EmailCode> EmailCodes { get; }
    DbSet<AiUsage> AiUsages { get; }
    DbSet<TelegramAccount> TelegramAccounts { get; }
    DbSet<TelegramLinkToken> TelegramLinkTokens { get; }
    DbSet<TelegramChat> TelegramChats { get; }
    DbSet<MockTest> MockTests { get; }
    DbSet<Group> Groups { get; }
    DbSet<GroupMember> GroupMembers { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<TelegramLogin> TelegramLogins { get; }
    DbSet<Feedback> Feedbacks { get; }

    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Saqlashda noyoblik buzildimi (parallel so'rov bir xil yozuvni yozib ulgurdi). Provayderga xos tekshiruv Infrastructure'da.</summary>
    bool IsUniqueViolation(DbUpdateException ex);
}
