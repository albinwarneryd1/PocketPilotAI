using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PocketPilotAI.Core.Domain.Entities;

namespace PocketPilotAI.Infrastructure.Persistence.Configurations;

public class MerchantConfig : IEntityTypeConfiguration<Merchant>
{
  public void Configure(EntityTypeBuilder<Merchant> builder)
  {
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
    builder.Property(x => x.NormalizedName).HasMaxLength(160).IsRequired();

    builder.HasIndex(x => new { x.UserId, x.NormalizedName }).IsUnique();

    builder.HasOne(x => x.DefaultCategory)
      .WithMany()
      .HasForeignKey(x => x.DefaultCategoryId)
      .OnDelete(DeleteBehavior.SetNull);
  }
}
