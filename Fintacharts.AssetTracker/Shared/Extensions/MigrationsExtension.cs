namespace Fintacharts.AssetTracker.Shared.Extensions;

using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class MigrationsExtension
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.Migrate();
    }
}