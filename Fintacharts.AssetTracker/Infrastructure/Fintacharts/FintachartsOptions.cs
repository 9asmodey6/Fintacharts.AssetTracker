namespace Fintacharts.AssetTracker.Infrastructure.Fintacharts;

public class FintachartsOptions
{
    public const string SectionName = "Fintacharts";
    
    public string BaseUrl { get; init; } = String.Empty;
    public string WssUrl { get; init; } = String.Empty;
    public string Username  { get; init; } = String.Empty;
    public string Password  { get; init; } = String.Empty;
    public string Realm { get; init; } = String.Empty;
    public string ClientId { get; init; } = String.Empty;
}