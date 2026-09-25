using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
        });
    }
}
