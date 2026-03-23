using Fintacharts.AssetTracker.Bootstrap;
using Fintacharts.AssetTracker.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer()
    .AddDatabaseServices(builder.Configuration)
    .AddFintachartsServices(builder.Configuration)
    .AddFeatureServices()
    .AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapEndpointsGenerated();

app.Run();