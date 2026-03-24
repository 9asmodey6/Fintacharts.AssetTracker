namespace Fintacharts.AssetTracker.Infrastructure.Persistence.Configurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        
        builder.Property(x => x.Symbol).IsRequired();
        builder.Property(x => x.Provider).IsRequired();
        builder.Property(x => x.Kind).IsRequired();
    }
    
}