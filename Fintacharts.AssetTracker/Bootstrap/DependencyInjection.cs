namespace Fintacharts.AssetTracker.Bootstrap;

using Features.GetAssets;
using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceScan.SourceGenerator;
using Shared.Interfaces;

public static partial class DependencyInjection
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default"))
                .UseSnakeCaseNamingConvention());

        return services;
    }

    public static IServiceCollection AddFintachartsServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FintachartsOptions>(
            configuration.GetSection(FintachartsOptions.SectionName));

        services.AddHttpClient<FintachartsTokenManager>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FintachartsOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }
        });

        services.AddSingleton<FintachartsTokenManager>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(nameof(FintachartsTokenManager));
            var options = sp.GetRequiredService<IOptions<FintachartsOptions>>();
            var logger = sp.GetRequiredService<ILogger<FintachartsTokenManager>>();

            return new FintachartsTokenManager(options, client, logger);
        });
        
        services.AddHttpClient<FintachartsRestClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FintachartsOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                client.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }

    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<GetAssetsHandler>();

        return services;
    }

    [GenerateServiceRegistrations(AssignableTo = typeof(IEndpoint), CustomHandler = "MapEndpoint")]
    public static partial IEndpointRouteBuilder MapEndpointsGenerated(this IEndpointRouteBuilder endpoints);
}