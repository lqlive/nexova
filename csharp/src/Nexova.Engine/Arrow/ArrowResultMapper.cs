using Apache.Arrow;
using Apache.Arrow.Types;
using Nexova.Engine.Contracts;

namespace Nexova.Engine.Arrow;

/// <summary>
/// Converts Arrow schema/record batches into the engine's JSON-friendly column and row shapes,
/// mirroring the Rust engine's <c>columns_from_fields</c> and <c>batches_to_rows</c>.
/// </summary>
public static class ArrowResultMapper
{
    public static IReadOnlyList<ColumnInfo> Columns(Schema schema)
    {
        var columns = new List<ColumnInfo>(schema.FieldsList.Count);
        foreach (var field in schema.FieldsList)
        {
            columns.Add(new ColumnInfo
            {
                Name = field.Name,
                ColumnType = TypeName(field.DataType),
                Nullable = field.IsNullable,
                Precision = field.DataType is Decimal128Type d128 ? (byte)d128.Precision
                    : field.DataType is Decimal256Type d256 ? (byte)d256.Precision
                    : null,
                Scale = field.DataType is Decimal128Type s128 ? (sbyte)s128.Scale
                    : field.DataType is Decimal256Type s256 ? (sbyte)s256.Scale
                    : null,
            });
        }

        return columns;
    }

    public static List<IReadOnlyList<object?>> Rows(IReadOnlyList<ColumnInfo> columns, IReadOnlyList<RecordBatch> batches)
    {
        var rows = new List<IReadOnlyList<object?>>();
        foreach (var batch in batches)
        {
            var arrays = new IArrowArray[batch.ColumnCount];
            for (var c = 0; c < batch.ColumnCount; c++)
            {
                arrays[c] = batch.Column(c);
            }

            for (var r = 0; r < batch.Length; r++)
            {
                var row = new object?[columns.Count];
                for (var c = 0; c < columns.Count && c < arrays.Length; c++)
                {
                    row[c] = GetValue(arrays[c], r);
                }

                rows.Add(row);
            }
        }

        return rows;
    }

    private static object? GetValue(IArrowArray array, int index)
    {
        if (array is null || !array.IsValid(index))
        {
            return null;
        }

        return array switch
        {
            BooleanArray a => a.GetValue(index),
            Int8Array a => a.GetValue(index),
            Int16Array a => a.GetValue(index),
            Int32Array a => a.GetValue(index),
            Int64Array a => a.GetValue(index),
            UInt8Array a => a.GetValue(index),
            UInt16Array a => a.GetValue(index),
            UInt32Array a => a.GetValue(index),
            UInt64Array a => a.GetValue(index),
            FloatArray a => a.GetValue(index),
            DoubleArray a => a.GetValue(index),
            Decimal128Array a => Decimal(() => a.GetValue(index), () => a.GetString(index)),
            Decimal256Array a => a.GetString(index),
            StringArray a => a.GetString(index),
            LargeStringArray a => a.GetString(index),
            Date32Array a => a.GetDateTime(index)?.ToString("yyyy-MM-dd"),
            Date64Array a => a.GetDateTime(index)?.ToString("O"),
            TimestampArray a => a.GetTimestamp(index)?.UtcDateTime.ToString("O"),
            Time32Array a => a.GetValue(index),
            Time64Array a => a.GetValue(index),
            BinaryArray a => Convert.ToBase64String(a.GetBytes(index).ToArray()),
            _ => null,
        };
    }

    private static object? Decimal(Func<decimal?> primary, Func<string?> fallback)
    {
        try
        {
            return primary();
        }
        catch (OverflowException)
        {
            return fallback();
        }
    }

    private static string TypeName(IArrowType type) => type switch
    {
        Decimal128Type d => $"Decimal128({d.Precision}, {d.Scale})",
        Decimal256Type d => $"Decimal256({d.Precision}, {d.Scale})",
        TimestampType t => $"Timestamp({t.Unit}, {(string.IsNullOrEmpty(t.Timezone) ? "None" : t.Timezone)})",
        _ => type.Name,
    };
}
