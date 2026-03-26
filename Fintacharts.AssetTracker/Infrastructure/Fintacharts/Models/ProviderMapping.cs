namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts.Models;

using System.Text.Json.Serialization;

public record ProviderMapping(
    [property: JsonPropertyName("symbol")] string Symbol);