var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();
var linksDb = postgres.AddDatabase("linksdb");
var analyticsDb = postgres.AddDatabase("analyticsdb");
var redis = builder.AddRedis("redis");
var messaging = builder.AddRabbitMQ("messaging").WithManagementPlugin();

var links = builder.AddProject<Projects.Shortener_Links_Api>("links")
    .WithReference(linksDb)
    .WaitFor(linksDb);

// No WaitFor(links) or WaitFor(messaging): Redirect must start and keep
// serving cached links while Links is down, and publishing a click is
// best-effort, so a slow-to-start broker must not block the hot path either.
builder.AddProject<Projects.Shortener_Redirect_Api>("redirect")
    .WithReference(redis)
    .WithReference(links)
    .WithReference(messaging)
    .WaitFor(redis);

builder.AddProject<Projects.Shortener_Analytics_Api>("analytics")
    .WithReference(analyticsDb)
    .WaitFor(analyticsDb)
    .WithReference(messaging);

builder.Build().Run();
