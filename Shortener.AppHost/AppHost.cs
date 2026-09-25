var builder = DistributedApplication.CreateBuilder(args);

var linksDb = builder.AddPostgres("postgres").WithDataVolume().AddDatabase("linksdb");
var redis = builder.AddRedis("redis");

var links = builder.AddProject<Projects.Shortener_Links_Api>("links")
    .WithReference(linksDb)
    .WaitFor(linksDb);

// No WaitFor(links): Redirect must start and keep serving cached links while
// Links is down, not block startup on it.
builder.AddProject<Projects.Shortener_Redirect_Api>("redirect")
    .WithReference(redis)
    .WithReference(links)
    .WaitFor(redis);

builder.Build().Run();
