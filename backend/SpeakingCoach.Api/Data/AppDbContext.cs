using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<PendingExercise> PendingExercises => Set<PendingExercise>();
    public DbSet<ReviewCard> ReviewCards => Set<ReviewCard>();
    public DbSet<ReviewLog> ReviewLogs => Set<ReviewLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReviewCard>(entity =>
        {
            entity.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.Source).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.Front).HasMaxLength(500);
            entity.Property(c => c.Back).HasMaxLength(500);
            entity.Property(c => c.Note).HasMaxLength(1000);
            entity.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

            // "Navbatdagi kartalar" so'rovi: WHERE UserId = ? AND DueAtUtc <= now ORDER BY DueAtUtc.
            entity.HasIndex(c => new { c.UserId, c.DueAtUtc });
            // Takror qo'shmaslik uchun tekshiruv: WHERE UserId = ? AND Front IN (...).
            // Unique EMAS: bir vaqtda kelgan ikki so'rov tufayli butun mashq
            // natijasi saqlanmay qolmasin — takrorni kod oldini oladi.
            entity.HasIndex(c => new { c.UserId, c.Front });
        });

        modelBuilder.Entity<ReviewLog>(entity =>
        {
            entity.HasOne<ReviewCard>().WithMany().HasForeignKey(l => l.CardId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(l => new { l.UserId, l.ReviewedAtUtc });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(256);
            // Bitta email bilan ikki marta ro'yxatdan o'tib bo'lmasin — bu
            // cheklov bazaning o'zida (unique index), faqat C# kodida emas:
            // bir vaqtda kelgan ikki so'rov ham ikkinchi nusxani yarata olmaydi.
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.Property(s => s.TokenHash).HasMaxLength(64); // SHA-256 = 64 ta hex belgi
            entity.HasIndex(s => s.TokenHash).IsUnique();
            entity.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PendingExercise>(entity =>
        {
            entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.Payload).HasColumnType("jsonb");
            entity.HasIndex(p => p.CreatedAtUtc);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            // Enum'ni raqam o'rniga matn sifatida saqlaymiz (masalan "Speaking"
            // emas 0) — jadvalni to'g'ridan-to'g'ri ko'rganda tushunarli bo'lishi
            // uchun, va kelajakda enum tartibi o'zgarsa ham mavjud qatorlar
            // buzilmasligi uchun (raqamli enum bo'lsa, tartib o'zgarganda barcha
            // eski qatorlar noto'g'ri qiymatga aylanib qoladi).
            entity.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);

            // jsonb — Postgres'ning maxsus JSON ustun turi: oddiy matn (text)
            // emas, ichidagi JSON'ni so'rovlarda filtrlash/indekslash mumkin
            // bo'lgan tur. Hozircha shunchaki saqlash uchun ishlatyapmiz,
            // lekin kelajakda "faqat grammatika balli 70 dan past bo'lgan
            // urinishlarni topish" kabi so'rovlar kerak bo'lsa, shu tur asqotadi.
            entity.Property(a => a.PromptData).HasColumnType("jsonb");
            entity.Property(a => a.ResponseData).HasColumnType("jsonb");

            entity.HasIndex(a => a.CreatedAtUtc);

            // Har bir yozuv (ixtiyoriy) foydalanuvchiga bog'lanadi. Tarix
            // so'rovi aynan shu uchta ustun bo'yicha filtrlaydi va saralaydi
            // (WHERE UserId = ? AND Type = ? ORDER BY CreatedAtUtc), shuning
            // uchun ular uchun bitta birlashgan (composite) indeks.
            entity.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(a => new { a.UserId, a.Type, a.CreatedAtUtc });
        });
    }
}
