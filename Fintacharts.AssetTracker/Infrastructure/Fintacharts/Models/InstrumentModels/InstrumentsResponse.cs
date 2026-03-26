namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts.Models.InstrumentModels;

using System.Text.Json.Serialization;

internal record InstrumentsResponse(
    [property: JsonPropertyName("data")] List<InstrumentDto> Data);