namespace Fintacharts.AssetTracker.Bootstrap;

using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public static class DependencyInjection
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

        return services;
    }
}