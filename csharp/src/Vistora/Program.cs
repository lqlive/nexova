using Vistora.Core.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDataSourceStore, InMemoryDataSourceStore>();
builder.Services.AddSingleton<IDatasetStore, InMemoryDatasetStore>();

var app = builder.Build();

await app.RunAsync();