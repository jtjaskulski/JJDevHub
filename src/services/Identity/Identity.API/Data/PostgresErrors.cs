using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace JJDevHub.Api.Data
{
    internal static class PostgresErrors
    {
        public static bool IsUniqueViolation(DbUpdateException exception)
        {
            for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
            {
                if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
