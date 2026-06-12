namespace Nexova.Core.Entities;

public class Dataset
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Sql { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
