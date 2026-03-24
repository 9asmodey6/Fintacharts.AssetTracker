namespace Fintacharts.AssetTracker.Infrastructure.Persistence.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AssetPriceConfiguration : IEntityTypeConfiguration<AssetPrice>
{
    public void Configure(EntityTypeBuilder<AssetPrice> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.InstrumentId)
            .IsUnique();

        builder.HasOne(x => x.Instrument)
            .WithMany(x => x.Prices)
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}