using Nexova.Core.Management;
using Nexova.Database.PostgreSql;
using Nexova.Storage.Aws;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNexovaCore()
    .AddInMemoryStore()
    .AddPostgreSqlDatabase()
    .AddFileStorage()
    .AddAwsS3Storage();

var app = builder.Build();

app.Run();