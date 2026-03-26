namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts.Models.InstrumentModels;

using System.Text.Json.Serialization;

public record InstrumentDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("mappings")] Dictionary<string, ProviderMapping> Mappings);