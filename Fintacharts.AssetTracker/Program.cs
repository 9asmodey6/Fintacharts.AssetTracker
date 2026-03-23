using Fintacharts.AssetTracker.Bootstrap;
using Fintacharts.AssetTracker.Infrastructure.Fintacharts;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer()
    .AddDatabaseServices(builder.Configuration)
    .AddFintachartsServices(builder.Configuration)
    .AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();