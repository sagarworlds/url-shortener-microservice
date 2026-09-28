using Xunit;

// Each test class here boots a full AppHost (Postgres, Redis, RabbitMQ, all
// three services) via Aspire.Hosting.Testing. xUnit parallelizes different
// test classes by default, and three of those graphs starting at once
// overloads Docker enough that health checks start timing out across the
// board. Capping to one thread serializes them.
[assembly: CollectionBehavior(MaxParallelThreads = 1)]
