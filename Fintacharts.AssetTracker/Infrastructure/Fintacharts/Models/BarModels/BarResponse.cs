namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts.Models.BarModels;

using System.Text.Json.Serialization;

internal record BarsResponse([property: JsonPropertyName("data")] List<BarDto> Data);