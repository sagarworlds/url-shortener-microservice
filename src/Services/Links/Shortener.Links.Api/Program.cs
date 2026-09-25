using Microsoft.EntityFrameworkCore;
using Shortener.Links.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddLinksInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    // Applying migrations automatically is a Development convenience only;
    // real environments apply them as an explicit deploy step.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LinksDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
