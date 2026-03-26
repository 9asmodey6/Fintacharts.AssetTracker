namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts.Models.BarModels;

using System.Text.Json.Serialization;

public record BarDto(
    [property: JsonPropertyName("t")] DateTime Timestamp,
    [property: JsonPropertyName("c")] decimal Close);