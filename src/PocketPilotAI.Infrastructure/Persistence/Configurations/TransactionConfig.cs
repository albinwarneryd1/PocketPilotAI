using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PocketPilotAI.Core.Domain.Entities;

namespace PocketPilotAI.Infrastructure.Persistence.Configurations;

public class TransactionConfig : IEntityTypeConfiguration<Transaction>
{
  public void Configure(EntityTypeBuilder<Transaction> builder)
  {
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Amount).HasPrecision(18, 2);
    builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
    builder.Property(x => x.Notes).HasMaxLength(500);

    builder.HasIndex(x => new { x.UserId, x.DateUtc });
    builder.HasIndex(x => new { x.UserId, x.CategoryId, x.DateUtc });

    builder.HasOne(x => x.User)
      .WithMany(x => x.Transactions)
      .HasForeignKey(x => x.UserId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(x => x.Account)
      .WithMany(x => x.Transactions)
      .HasForeignKey(x => x.AccountId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasOne(x => x.Merchant)
      .WithMany(x => x.Transactions)
      .HasForeignKey(x => x.MerchantId)
      .OnDelete(DeleteBehavior.SetNull);

    builder.HasOne(x => x.Category)
      .WithMany(x => x.Transactions)
      .HasForeignKey(x => x.CategoryId)
      .OnDelete(DeleteBehavior.SetNull);

    builder.HasOne(x => x.ParentTransaction)
      .WithMany(x => x.SplitChildren)
      .HasForeignKey(x => x.ParentTransactionId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}
