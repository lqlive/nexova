using Npgsql;
using Nexova.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Nexova.Database.PostgreSql;

public class PostgreSqlContext : AbstractContext<PostgreSqlContext>
{
    public override bool IsUniqueConstraintViolationException(DbUpdateException exception)
     => exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}