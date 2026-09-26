using Microsoft.EntityFrameworkCore;
using Shortener.Analytics.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAnalyticsInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    // Applying migrations automatically is a Development convenience only;
    // real environments apply them as an explicit deploy step.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
