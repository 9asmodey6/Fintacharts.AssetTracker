namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts;

using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Models;

public class FintachartsTokenManager(
    IOptions<FintachartsOptions> options,
    HttpClient client,
    ILogger<FintachartsTokenManager> logger)
{
    private string _accessToken = string.Empty;
    private DateTime _expiresAt = DateTime.MinValue;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_accessToken != string.Empty && DateTime.UtcNow < _expiresAt)
        {
            return _accessToken;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_accessToken != string.Empty && DateTime.UtcNow < _expiresAt)
            {
                return _accessToken;
            }

            await RefreshTokenAsync(ct);
            return _accessToken!;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshTokenAsync(CancellationToken ct)
    {
        logger.LogInformation("Refreshing Fintacharts token...");

        var body = new FormUrlEncodedContent([
            new("grant_type", "password"),
            new("client_id", options.Value.ClientId),
            new("username", options.Value.Username),
            new("password", options.Value.Password)
        ]);

        var url = $"identity/realms/{options.Value.Realm}/protocol/openid-connect/token";

        var response = await client.PostAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);

        _accessToken = json!.AccessToken;

        _expiresAt = DateTime.UtcNow;

        logger.LogInformation(
            "Token refreshed. Expires at {ExpiresAt}", _expiresAt);
    }
}