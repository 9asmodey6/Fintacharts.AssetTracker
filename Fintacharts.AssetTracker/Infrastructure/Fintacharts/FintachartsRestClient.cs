namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts;

using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Models;
using Models.BarModels;
using Models.InstrumentModels;
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
    
    public async Task<List<BarDto>> GetPriceHistoryAsync(
        string instrumentId, 
        int barsCount, 
        CancellationToken ct)
    {
        var token = await tokenManager.GetAccessTokenAsync(ct);
        
        var url = $"api/bars/v1/bars/count-back?instrumentId={instrumentId}" +
                  $"&provider={FintachartsConstants.DefaultProvider}" +
                  $"&interval=1&periodicity=minute&barsCount={barsCount}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BarsResponse>(ct);
        return result?.Data ?? [];
    }
}