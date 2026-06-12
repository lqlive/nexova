namespace Nexova.Core.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";
    public string? Type { get; set; } = InMemoryStoreOptions.Name;
    public string? ConnectionString { get; set; }
}
