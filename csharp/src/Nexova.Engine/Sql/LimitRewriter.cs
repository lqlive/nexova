using SqlParser;
using SqlParser.Ast;
using SqlParser.Dialects;
using Nexova.Engine.Contracts;

namespace Nexova.Engine.Sql;

/// <summary>
/// Applies the default/maximum row cap, mirroring the Rust engine's <c>limited_sql</c>.
/// A top-level <c>LIMIT</c>/<c>FETCH</c> already present in the query is preserved.
/// </summary>
public static class LimitRewriter
{
    public static string ApplyLimit(string sql, uint? limit)
    {
        var effective = Math.Min(limit ?? EngineConstants.DefaultLimit, EngineConstants.MaxLimit);
        var trimmed = Normalize(sql);

        return HasTopLevelLimit(trimmed)
            ? trimmed
            : $"{trimmed} LIMIT {effective}";
    }

    public static bool HasTopLevelLimit(string sql)
    {
        try
        {
            var statements = new Parser().ParseSql(sql, new GenericDialect());
            return statements.Count == 1
                && statements[0] is Statement.Select select
                && (select.Query.Limit is not null || select.Query.Fetch is not null);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Strips leading/trailing whitespace and any trailing statement terminator(s),
    /// matching the Rust engine's <c>normalize_sql</c>.
    /// </summary>
    private static string Normalize(string sql) =>
        sql.Trim().TrimEnd(';', ' ', '\t', '\r', '\n', '\f', '\v');
}
