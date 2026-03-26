namespace Fintacharts.AssetTracker.BackgroundServices.Models;

using System.Text.Json.Serialization;

internal record WsMessage(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("instrumentId")]
    string? InstrumentId,
    [property: JsonPropertyName("ask")] PriceData? Ask,
    [property: JsonPropertyName("bid")] PriceData? Bid,
    [property: JsonPropertyName("last")] PriceData? Last);