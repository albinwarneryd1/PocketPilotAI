using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PocketPilotAI.Core.Domain.Entities;

namespace PocketPilotAI.Infrastructure.Persistence.Configurations;

public class BudgetConfig : IEntityTypeConfiguration<Budget>
{
  public void Configure(EntityTypeBuilder<Budget> builder)
  {
    builder.HasKey(x => x.Id);
    builder.Property(x => x.PlannedAmount).HasPrecision(18, 2);
    builder.Property(x => x.AlertThresholdPercent).HasPrecision(5, 2);
    builder.HasIndex(x => new { x.UserId, x.CategoryId, x.Month }).IsUnique();

    builder.HasOne(x => x.User)
      .WithMany(x => x.Budgets)
      .HasForeignKey(x => x.UserId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(x => x.Category)
      .WithMany(x => x.Budgets)
      .HasForeignKey(x => x.CategoryId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}
