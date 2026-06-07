using Vistora.Core.Entities;

namespace Vistora.DataSources.Models;

public sealed record DataSourceRequest(
    string Name,
    string Type,
    DataSourceConfiguration? Configuration);
