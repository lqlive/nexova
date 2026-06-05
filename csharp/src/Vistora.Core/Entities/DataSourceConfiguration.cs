namespace Vistora.Core.Entities;

public class DataSourceConfiguration
{
    public string? ConnectionString { get; set; }

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? Database { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Schema { get; set; }

    public string? Path { get; set; }

    public string? Table { get; set; }

    public string? Alias { get; set; }

    public bool? HasHeader { get; set; }

    public string? Delimiter { get; set; }

    public string? Sheet { get; set; }

    public Dictionary<string, string?> Options { get; set; } = new();
}
