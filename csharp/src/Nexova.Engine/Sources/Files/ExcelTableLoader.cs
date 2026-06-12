using System.Text;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Apache.DataFusion;
using ExcelDataReader;
using Nexova.Engine.Exceptions;

namespace Nexova.Engine.Sources.Files;

/// <summary>
/// Reads an Excel worksheet into an in-memory Arrow table and exposes it as a
/// <see cref="SimpleTableProvider"/>. DataFusion has no native Excel reader, so (as in the Rust
/// engine, which used <c>calamine</c>) the sheet is materialised. Cells are read as strings.
/// </summary>
public static class ExcelTableLoader
{
    static ExcelTableLoader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static SimpleTableProvider LoadLocal(string path, string? sheet)
    {
        using var stream = File.OpenRead(path);
        return Load(stream, sheet);
    }

    public static SimpleTableProvider LoadBytes(byte[] bytes, string? sheet)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return Load(stream, sheet);
    }

    private static SimpleTableProvider Load(Stream stream, string? sheet)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream);
        SelectSheet(reader, sheet);

        if (!reader.Read())
        {
            throw EngineException.FileSource("excel sheet is empty");
        }

        var columnCount = reader.FieldCount;
        if (columnCount <= 0)
        {
            throw EngineException.FileSource("excel sheet has no columns");
        }

        var headers = new string[columnCount];
        for (var c = 0; c < columnCount; c++)
        {
            var value = reader.GetValue(c)?.ToString();
            headers[c] = string.IsNullOrWhiteSpace(value) ? $"column_{c + 1}" : value!;
        }

        var builders = new StringArray.Builder[columnCount];
        for (var c = 0; c < columnCount; c++)
        {
            builders[c] = new StringArray.Builder();
        }

        var rowCount = 0;
        while (reader.Read())
        {
            for (var c = 0; c < columnCount; c++)
            {
                var value = c < reader.FieldCount ? reader.GetValue(c) : null;
                if (value is null)
                {
                    builders[c].AppendNull();
                }
                else
                {
                    builders[c].Append(value.ToString() ?? string.Empty);
                }
            }

            rowCount++;
        }

        var fields = headers.Select(name => new Field(name, StringType.Default, nullable: true)).ToList();
        var schema = new Schema(fields, metadata: null);
        var arrays = builders.Select(builder => (IArrowArray)builder.Build()).ToList();
        var batch = new RecordBatch(schema, arrays, rowCount);

        return SimpleTableProvider.FromIpcStream(ToIpc(batch, schema));
    }

    private static void SelectSheet(IExcelDataReader reader, string? sheet)
    {
        if (string.IsNullOrWhiteSpace(sheet))
        {
            return;
        }

        do
        {
            if (string.Equals(reader.Name, sheet, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
        while (reader.NextResult());

        throw EngineException.FileSource($"excel sheet '{sheet}' not found");
    }

    private static byte[] ToIpc(RecordBatch batch, Schema schema)
    {
        using var memory = new MemoryStream();
        using (var writer = new ArrowStreamWriter(memory, schema))
        {
            writer.WriteStart();
            writer.WriteRecordBatch(batch);
            writer.WriteEnd();
        }

        return memory.ToArray();
    }
}
