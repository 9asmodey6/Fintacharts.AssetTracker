namespace Fintacharts.AssetTracker.Infrastructure.Persistence.Entities;

public class AssetPrice
{
    public int Id { get; set; }

    public string InstrumentId { get; set; } = string.Empty;

    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Last { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Instrument Instrument { get; set; } = null!;
}