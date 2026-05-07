using Fintacharts.AssetTracker.Bootstrap;
using Fintacharts.AssetTracker.Shared.Extensions;
using Fintacharts.AssetTracker.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services
    .RegisterServices()
    .RegisterDatabase(builder.Configuration)
    .RegisterConfigurationOptions(builder.Configuration)
    .ConfigureCors()
    .RegisterHttpClients(builder.Configuration)
    .RegisterValidators();

var app = builder.Build();

app.UseExceptionHandler(_ => { });

app.ApplyMigrations();

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapHub<PriceHub>("/hubs/prices");
app.MapEndpointsGenerated();

app.Run();