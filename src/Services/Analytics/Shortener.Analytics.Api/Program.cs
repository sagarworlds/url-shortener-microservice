using Microsoft.EntityFrameworkCore;
using Shortener.Analytics.Api.Endpoints;
using Shortener.Analytics.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAnalyticsInfrastructure();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();
app.MapAnalyticsEndpoints();

if (app.Environment.IsDevelopment())
{
    // Applying migrations automatically is a Development convenience only;
    // real environments apply them as an explicit deploy step.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
