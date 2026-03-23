namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts;

using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Shared.Consts;

public class FintachartsRestClient(
    FintachartsTokenManager tokenManager,
    HttpClient client,
    ILogger<FintachartsRestClient> logger)
{
    public async Task<List<InstrumentDto>> GetInstrumentsAsync(
        string provider = FintachartsConstants.DefaultProvider,
        string kind = FintachartsConstants.InstrumentKinds.Forex,
        CancellationToken ct = default)
    {
        logger.LogDebug("Fetching instruments from Fintacharts...");

        var token = await tokenManager.GetAccessTokenAsync(ct);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/instruments/v1/instruments?provider={provider}&kind={kind}&page=1&size=100");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<InstrumentsResponse>(ct);

        return result?.Data ?? [];
    }
}

internal record InstrumentsResponse(
    [property: JsonPropertyName("data")] List<InstrumentDto> Data);

public record InstrumentDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("mappings")] Dictionary<string, ProviderMapping> Mappings);

public record ProviderMapping(
    [property: JsonPropertyName("symbol")] string Symbol);