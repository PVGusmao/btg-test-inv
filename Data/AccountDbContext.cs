using Microsoft.EntityFrameworkCore;
using ProjetoBtg.Api.Events;
using ProjetoBtg.Api.Models;

namespace ProjetoBtg.Api.Data;

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AreaNotification> AreaNotifications => Set<AreaNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<Account>();
        account.ToTable("Accounts");
        account.HasKey(item => item.Id);
        account.Property(item => item.Id).ValueGeneratedNever();
        account.Property(item => item.HolderName).HasMaxLength(150).IsRequired();
        account.Property(item => item.TaxId).HasMaxLength(11).IsRequired();
        account.Property(item => item.Deactivated)
            .HasColumnName("Desativado")
            .HasDefaultValue(0)
            .IsRequired();
        account.HasIndex(item => item.TaxId)
            .IsUnique()
            .HasFilter("\"Desativado\" = 0");
        account.Property(item => item.Status).IsRequired();
        account.Property(item => item.FraudPreventionValidation).IsRequired();
        account.Property(item => item.CardValidation).IsRequired();

        var notification = modelBuilder.Entity<AreaNotification>();
        notification.ToTable("AreaNotifications");
        notification.HasKey(item => item.Id);
        notification.Property(item => item.Id).ValueGeneratedNever();
        notification.Property(item => item.Area).HasMaxLength(100).IsRequired();
        notification.Property(item => item.AccountId).IsRequired();
        notification.Property(item => item.Operation).IsRequired();
        notification.Property(item => item.ReceivedAt).IsRequired();
        notification.HasIndex(item => item.AccountId);
    }
}
