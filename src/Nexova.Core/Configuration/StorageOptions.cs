namespace Nexova.Core.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public string? Type { get; set; } = FileSystemStorageOptions.Name;
}
