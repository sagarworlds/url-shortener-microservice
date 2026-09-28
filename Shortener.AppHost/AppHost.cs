var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();
var linksDb = postgres.AddDatabase("linksdb");
var analyticsDb = postgres.AddDatabase("analyticsdb");
var redis = builder.AddRedis("redis");
var messaging = builder.AddRabbitMQ("messaging").WithManagementPlugin();

var links = builder.AddProject<Projects.Shortener_Links_Api>("links")
    .WithReference(linksDb)
    .WaitFor(linksDb)
    .WithReference(messaging);

// No WithReference(links): Redirect resolves codes from its own Redis read
// model now, fed by Links' outbox over messaging, not by calling Links
// directly. No WaitFor(messaging) either: publishing/consuming is
// best-effort, so a slow-to-start broker must not block startup.
var redirect = builder.AddProject<Projects.Shortener_Redirect_Api>("redirect")
    .WithReference(redis)
    .WithReference(messaging)
    .WaitFor(redis);

var analytics = builder.AddProject<Projects.Shortener_Analytics_Api>("analytics")
    .WithReference(analyticsDb)
    .WaitFor(analyticsDb)
    .WithReference(messaging);

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak")
    .WithArgs("start-dev", "--import-realm")
    .WithBindMount("./KeycloakConfiguration", "/opt/keycloak/data/import")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http");

builder.AddProject<Projects.Shortener_Gateway>("gateway")
    .WithReference(links)
    .WithReference(redirect)
    .WithReference(analytics)
    .WithExternalHttpEndpoints();

builder.Build().Run();
