using Nexova.Core.Management;
using Nexova.Database.PostgreSql;
using Nexova.DataSources;
using Nexova.DataSources.Http;
using Nexova.Storage.Aws;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNexovaCore()
    .AddInMemoryStore()
    .AddPostgreSqlDatabase()
    .AddFileStorage()
    .AddAwsS3Storage();

builder.Services.AddScoped<DataSourceService>();

var app = builder.Build();

app.MapDataSourceApi();

app.Run();