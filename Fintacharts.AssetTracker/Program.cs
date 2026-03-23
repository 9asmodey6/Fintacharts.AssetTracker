using Fintacharts.AssetTracker.Bootstrap;

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapEndpointsGenerated();

app.Run();