namespace Fintacharts.AssetTracker.Bootstrap;

using BackgroundServices;
using Features.GetAssets;
using Features.GetPriceHistory;
using Features.GetPrices;
using FluentValidation;
using Infrastructure.Cache;
using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceScan.SourceGenerator;
using Shared.Events;
using Shared.Interfaces;

public static partial class DependencyInjection
{
    public static IServiceCollection RegisterDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default"))
                .UseSnakeCaseNamingConvention());

        return services;
    }

    public static IServiceCollection RegisterConfigurationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FintachartsOptions>(
            configuration.GetSection(FintachartsOptions.SectionName));

        return services;
    }

    public static IServiceCollection RegisterHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(FintachartsOptions.SectionName)
            .Get<FintachartsOptions>()!;

        services.AddHttpClient(nameof(FintachartsTokenManager),
            client => { client.BaseAddress = new Uri(options.BaseUrl); });

        services.AddHttpClient<FintachartsRestClient>(client => { client.BaseAddress = new Uri(options.BaseUrl); });

        return services;
    }

    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddSingleton<PriceCache>();

        services.AddSingleton<IEventBus, InMemoryEventBus>();


        services.AddScoped<GetAssetsHandler>();

        services.AddHostedService<PriceUpdateWorker>();

        services.AddSingleton<FintachartsTokenManager>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(nameof(FintachartsTokenManager));
            var options = sp.GetRequiredService<IOptions<FintachartsOptions>>();
            var logger = sp.GetRequiredService<ILogger<FintachartsTokenManager>>();

            return new FintachartsTokenManager(options, client, logger);
        });

        services.AddScoped<GetPricesHandler>();

        services.AddScoped<GetPriceHistoryHandler>();

        return services;
    }

    [GenerateServiceRegistrations(AssignableTo = typeof(IEndpoint), CustomHandler = "MapEndpoint")]
    public static partial IEndpointRouteBuilder MapEndpointsGenerated(this IEndpointRouteBuilder endpoints);

    [GenerateServiceRegistrations(AssignableTo = typeof(IValidator<>), Lifetime = ServiceLifetime.Scoped)]
    public static partial IServiceCollection RegisterValidators(this IServiceCollection services);
}