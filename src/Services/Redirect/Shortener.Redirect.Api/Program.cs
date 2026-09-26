using Shortener.Redirect.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedirectInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
