using Fintacharts.AssetTracker.Bootstrap;
using Fintacharts.AssetTracker.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services
    .RegisterBasicServices()
    .RegisterDatabase(builder.Configuration)
    .RegisterConfigurationOptions(builder.Configuration)
    .RegisterHttpClients(builder.Configuration)
    .RegisterValidators()
    .RegisterServices();

var app = builder.Build();

app.ApplyMigrations();

if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapEndpointsGenerated();

app.Run();