namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts;

using System.Text.Json.Serialization;

public class FintachartsTokenManager(FintachartsOptions options, HttpClient client, ILogger<FintachartsTokenManager> logger)
{
    private string? _accessToken;
    private DateTime _expiresAt = DateTime.MinValue;

    private readonly SemaphoreSlim _lock = new(1, 1);

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_accessToken is not null && DateTime.UtcNow < _expiresAt)
        {
            return _accessToken;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTime.UtcNow < _expiresAt)
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
            new("client_id", options.ClientId),
            new("username", options.Username),
            new("password", options.Password)
        ]);
        
        var url = $"{options.BaseUrl}/identity/realms/{options.Realm}" +
                  $"/protocol/openid-connect/token";
        
        var response = await client.PostAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        
        _expiresAt = DateTime.UtcNow.AddSeconds(json!.ExpiresIn - 300);
        
        logger.LogInformation(
            "Token refreshed. Expires at {ExpiresAt}", _expiresAt);
    }
}

internal record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);