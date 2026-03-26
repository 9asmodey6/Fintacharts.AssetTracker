namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts.Models;

using System.Text.Json.Serialization;

internal record TokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn);