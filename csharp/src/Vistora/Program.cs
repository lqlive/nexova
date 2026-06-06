using Vistora.Database.PostgreSQL.Management;

var builder = WebApplication.CreateBuilder(args);

var postgreSQLConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");
if (!string.IsNullOrWhiteSpace(postgreSQLConnectionString))
{
    builder.Services.AddPostgreSQLDatabase(postgreSQLConnectionString);
}

var app = builder.Build();

await app.RunAsync();