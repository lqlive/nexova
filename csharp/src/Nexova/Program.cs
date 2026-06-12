using FluentValidation;
using Nexova.Core.Extensions;
using Nexova.DataSources;
using Nexova.DataSources.Http;
using Nexova.Datasets;
using Nexova.Datasets.Http;
using Nexova.Engine.Extensions;
using Nexova.Query.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNexovaCore();
builder.Services.AddSingleton<DataSourceService>();
builder.Services.AddSingleton<DatasetService>();

builder.Services.AddNexovaEngine(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.MapDataSourceApiV1();
app.MapDatasetApiV1();
app.MapQueryApiV1();

await app.RunAsync();