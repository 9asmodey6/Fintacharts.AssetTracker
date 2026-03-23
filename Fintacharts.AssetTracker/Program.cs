using Fintacharts.AssetTracker.Infrastructure.Fintacharts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FintachartsOptions>(
    builder.Configuration.GetSection(FintachartsOptions.SectionName));

builder.Services.AddHttpClient<FintachartsTokenManager>();
builder.Services.AddSingleton<FintachartsTokenManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/test-token", async (FintachartsTokenManager tokenManager) =>
{
    var token = await tokenManager.GetAccessTokenAsync();
    // Показываем только первые 50 символов — токен длинный
    return Results.Ok(new { preview = token[..50] + "..." });
});

app.Run();

