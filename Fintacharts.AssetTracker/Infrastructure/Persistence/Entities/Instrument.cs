namespace Fintacharts.AssetTracker.Infrastructure.Persistence.Entities;

public class Instrument
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;

    public ICollection<AssetPrice> Prices { get; set; } = [];
}