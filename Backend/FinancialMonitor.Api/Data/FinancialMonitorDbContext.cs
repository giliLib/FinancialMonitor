
using FinancialMonitor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialMonitor.Api.Data;

public class FinancialMonitorDbContext : DbContext
{
    public FinancialMonitorDbContext(
        DbContextOptions<FinancialMonitorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.TransactionId);

            entity.Property(t => t.TransactionId)
                .ValueGeneratedNever();

            entity.Property(t => t.Amount)
                .HasColumnType("numeric")
                .IsRequired();

            entity.Property(t => t.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Normalize DateTimeOffset to UTC for PostgreSQL
            entity.Property(t => t.Timestamp)
                .HasConversion(
                    value => value.UtcDateTime,
                    value => new DateTimeOffset(
                        DateTime.SpecifyKind(value, DateTimeKind.Utc)
                    )
                )
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });
    }
}
