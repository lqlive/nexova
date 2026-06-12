using Apache.DataFusion;
using Nexova.Engine.Contracts;

namespace Nexova.Engine.Sources.Files;

/// <summary>
/// Builds DataFusion <see cref="CsvReadOptions"/> from a source's CSV settings
/// (<c>hasHeader</c>/<c>delimiter</c>), mirroring the Rust engine's CSV registration.
/// </summary>
public static class CsvOptionsFactory
{
    public static CsvReadOptions Build(DataSourceConnection source)
    {
        // NOTE: schemaOverride parity (explicit column types via SchemaIpc) is not yet wired here;
        // DataFusion's CSV type inference is used. Tracked as a follow-up to the Rust engine.
        return new CsvReadOptions
        {
            HasHeader = source.HasHeader ?? true,
            Delimiter = DelimiterByte(source.Delimiter),
        };
    }

    private static byte DelimiterByte(string? delimiter)
    {
        if (string.IsNullOrEmpty(delimiter))
        {
            return (byte)',';
        }

        var c = delimiter[0];
        return c <= 0x7F ? (byte)c : (byte)',';
    }
}
