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
            entity.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(a => a.PromptData).HasColumnType("jsonb");
            entity.Property(a => a.ResponseData).HasColumnType("jsonb");
            entity.HasIndex(a => a.CreatedAtUtc);
        });
    }
}
