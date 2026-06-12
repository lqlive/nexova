namespace Nexova.Core.Configuration;

public class FileSystemStorageOptions
{
    public const string Name = "FileSystem";
    public const string SectionName = "FileSystemStorage";
    public string Path { get; set; } = "storage";
}