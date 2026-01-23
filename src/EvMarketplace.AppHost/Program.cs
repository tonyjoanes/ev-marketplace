var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var evDb = postgres.AddDatabase("evmarketplace");

// Add the API project
var api = builder.AddProject<Projects.EvMarketplace_Api>("api")
    .WithReference(evDb)
    .WaitFor(evDb);

builder.Build().Run();
