using SqlParser;
using SqlParser.Ast;
using SqlParser.Dialects;
using Nexova.Engine.Exceptions;

namespace Nexova.Engine.Sql;

/// <summary>
/// Enforces a single, read-only <c>SELECT</c>/<c>WITH</c> statement using the SqlParserCS AST
/// (the C# port of the same <c>sqlparser</c> crate used by the Rust engine). Faithful port of
/// the Rust engine's <c>validate_readonly_sql</c>.
/// </summary>
public static class ReadOnlySqlValidator
{
    public static void Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw EngineException.SqlSyntax("SQL query is empty");
        }

        Sequence<Statement> statements;
        try
        {
            statements = new Parser().ParseSql(sql, new GenericDialect());
        }
        catch (Exception exception) when (exception is not EngineException)
        {
            throw EngineException.SqlSyntax(exception.Message);
        }

        if (statements.Count != 1)
        {
            throw EngineException.ReadOnlyViolation("exactly one SQL statement is allowed");
        }

        if (statements[0] is Statement.Select select && IsReadOnlyQuery(select.Query))
        {
            return;
        }

        throw EngineException.ReadOnlyViolation("only read-only SELECT queries are allowed");
    }

    private static bool IsReadOnlyQuery(Query query)
    {
        if (query.Locks is { Count: > 0 })
        {
            return false;
        }

        if (query.With is not null && query.With.CteTables.Any(cte => !IsReadOnlyQuery(cte.Query)))
        {
            return false;
        }

        return IsReadOnlySetExpression(query.Body);
    }

    private static bool IsReadOnlySetExpression(SetExpression body) => body switch
    {
        SetExpression.SelectExpression select => select.Select.Into is null,
        SetExpression.QueryExpression queryExpression => IsReadOnlyQuery(queryExpression.Query),
        SetExpression.SetOperation setOperation =>
            IsReadOnlySetExpression(setOperation.Left) && IsReadOnlySetExpression(setOperation.Right),
        _ => false,
    };
}
