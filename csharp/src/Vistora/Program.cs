using FluentValidation;
using Vistora.Core.Extensions;
using Vistora.DataSources;
using Vistora.DataSources.Http;
using Vistora.Datasets;
using Vistora.Datasets.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVistoraCore();
builder.Services.AddSingleton<DataSourceService>();
builder.Services.AddSingleton<DatasetService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.MapDataSourceApiV1();
app.MapDatasetApiV1();

await app.RunAsync();