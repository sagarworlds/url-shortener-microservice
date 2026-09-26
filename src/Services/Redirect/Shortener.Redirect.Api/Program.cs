using Shortener.Redirect.Api.Endpoints;
using Shortener.Redirect.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedirectInfrastructure();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();
app.MapRedirectEndpoints();

app.Run();
