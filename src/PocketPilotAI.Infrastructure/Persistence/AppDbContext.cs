using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Domain.Entities;

namespace PocketPilotAI.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();

  public DbSet<Account> Accounts => Set<Account>();

  public DbSet<Transaction> Transactions => Set<Transaction>();

  public DbSet<Category> Categories => Set<Category>();

  public DbSet<Budget> Budgets => Set<Budget>();

  public DbSet<Merchant> Merchants => Set<Merchant>();

  public DbSet<RecurringPayment> RecurringPayments => Set<RecurringPayment>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    modelBuilder.Entity<User>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.HasIndex(x => x.Email).IsUnique();
      entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
      entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
      entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
    });

    modelBuilder.Entity<Account>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
      entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
      entity.HasOne(x => x.User)
        .WithMany(x => x.Accounts)
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<Category>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
      entity.Property(x => x.ColorHex).HasMaxLength(12).IsRequired();
      entity.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
      entity.HasOne(x => x.User)
        .WithMany(x => x.Categories)
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(x => x.ParentCategory)
        .WithMany(x => x.SubCategories)
        .HasForeignKey(x => x.ParentCategoryId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<RecurringPayment>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.Frequency).HasMaxLength(40).IsRequired();
      entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
      entity.HasOne(x => x.User)
        .WithMany(x => x.RecurringPayments)
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(x => x.Account)
        .WithMany(x => x.RecurringPayments)
        .HasForeignKey(x => x.AccountId)
        .OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(x => x.Category)
        .WithMany()
        .HasForeignKey(x => x.CategoryId)
        .OnDelete(DeleteBehavior.SetNull);
      entity.HasOne(x => x.Merchant)
        .WithMany(x => x.RecurringPayments)
        .HasForeignKey(x => x.MerchantId)
        .OnDelete(DeleteBehavior.SetNull);
    });
  }
}
