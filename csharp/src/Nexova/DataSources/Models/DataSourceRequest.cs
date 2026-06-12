using Nexova.Core.Entities;

namespace Nexova.DataSources.Models;

public sealed record DataSourceRequest(
    string Name,
    string Type,
    DataSourceConfiguration? Configuration);
