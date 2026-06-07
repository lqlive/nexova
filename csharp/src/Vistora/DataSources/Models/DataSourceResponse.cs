using Vistora.Core.Entities;

namespace Vistora.DataSources.Models;

public sealed record DataSourceResponse(
    Guid Id,
    string Name,
    string Type,
    DataSourceConfiguration Configuration,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
