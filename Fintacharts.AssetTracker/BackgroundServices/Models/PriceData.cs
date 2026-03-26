namespace Fintacharts.AssetTracker.BackgroundServices.Models;

using System.Text.Json.Serialization;

internal record PriceData(
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("timestamp")]
    DateTime Timestamp);