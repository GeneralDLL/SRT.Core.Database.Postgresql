using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SRT.Core.Database.Postgresql.UnitOfWorks
{
    /// <summary>
    /// Startup sequence maintenance for PostgreSQL.
    /// Never setval downward at startup. Gaps where current &gt; maxId are logged only.
    /// When the sequence is behind maxId and increment is positive, setval upward only.
    /// Sequences do not guarantee gapless numbering (rollbacks create gaps).
    /// </summary>
    internal static class CheckSequenceGap
    {
        #region Main Method

        public static async Task SRT_FixIdentityGapsAsync<AppDbContext>(this WebApplication app)
            where AppDbContext : DbContext
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("SRT.Core.Database.Postgresql.CheckSequenceGap");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = db.Database.GetDbConnection();

            await connection.OpenAsync().ConfigureAwait(false);

            try
            {
                var tables = await LoadSequenceTablesAsync(connection).ConfigureAwait(false);

                foreach (var table in tables)
                {
                    try
                    {
                        await ProcessTableAsync(connection, table, logger).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Sequence maintenance failed for {Schema}.{Table}",
                            table.SchemaName,
                            table.TableName);
                    }
                }
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region Load / Process

        private static async Task<List<SequenceTableInfo>> LoadSequenceTablesAsync(
            System.Data.Common.DbConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = """
SELECT
    n.nspname AS schema_name,
    t.relname AS table_name,
    a.attname AS column_name,
    pg_get_serial_sequence(format('%I.%I', n.nspname, t.relname), a.attname) AS sequence_name
FROM pg_class t
JOIN pg_namespace n ON n.oid = t.relnamespace
JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum > 0 AND NOT a.attisdropped
WHERE t.relkind = 'r'
  AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
  AND t.relname NOT LIKE 'pg_%'
  AND t.relname NOT LIKE '__EFMigrationsHistory%'
  AND pg_get_serial_sequence(format('%I.%I', n.nspname, t.relname), a.attname) IS NOT NULL;
""";

            var tables = new List<SequenceTableInfo>();
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var sequenceName = reader.GetString(3);
                var (schema, name) = SplitSequenceName(sequenceName);
                tables.Add(new SequenceTableInfo
                {
                    SchemaName = reader.GetString(0),
                    TableName = reader.GetString(1),
                    ColumnName = reader.GetString(2),
                    SequenceSchema = schema,
                    SequenceName = name,
                });
            }

            return tables;
        }

        private static async Task ProcessTableAsync(
            System.Data.Common.DbConnection connection,
            SequenceTableInfo table,
            ILogger logger)
        {
            var qualified = QuoteQualifiedName(table.SchemaName, table.TableName);
            var column = QuoteIdentifier(table.ColumnName);

            var maxCommand = connection.CreateCommand();
            maxCommand.CommandText = $"SELECT COALESCE(MAX({column}), 0) FROM {qualified}";
            var maxObj = await maxCommand.ExecuteScalarAsync().ConfigureAwait(false);
            var maxId = Convert.ToInt64(maxObj);

            var seqCommand = connection.CreateCommand();
            seqCommand.CommandText = """
SELECT COALESCE(last_value, 0), COALESCE(increment_by, 1)
FROM pg_sequences
WHERE schemaname = @schema AND sequencename = @name;
""";
            var schemaParam = seqCommand.CreateParameter();
            schemaParam.ParameterName = "schema";
            schemaParam.Value = table.SequenceSchema;
            seqCommand.Parameters.Add(schemaParam);
            var nameParam = seqCommand.CreateParameter();
            nameParam.ParameterName = "name";
            nameParam.Value = table.SequenceName;
            seqCommand.Parameters.Add(nameParam);

            long currentIdentity = 0;
            long incrementValue = 1;
            await using (var seqReader = await seqCommand.ExecuteReaderAsync().ConfigureAwait(false))
            {
                if (await seqReader.ReadAsync().ConfigureAwait(false))
                {
                    currentIdentity = Convert.ToInt64(seqReader.GetValue(0));
                    incrementValue = Convert.ToInt64(seqReader.GetValue(1));
                }
            }

            if (currentIdentity > maxId)
            {
                logger.LogInformation(
                    "Sequence ahead of data (log only): {Schema}.{Table}.{Column} current={CurrentIdentity} maxId={MaxId}",
                    table.SchemaName,
                    table.TableName,
                    table.ColumnName,
                    currentIdentity,
                    maxId);
                return;
            }

            if (incrementValue > 0 && currentIdentity < maxId)
            {
                logger.LogWarning(
                    "Setting sequence upward: {Schema}.{Table}.{Column} from {CurrentIdentity} to {NewSeed} (maxId={MaxId})",
                    table.SchemaName,
                    table.TableName,
                    table.ColumnName,
                    currentIdentity,
                    maxId,
                    maxId);

                var setvalCommand = connection.CreateCommand();
                setvalCommand.CommandText =
                    $"SELECT setval({QuoteLiteral(table.SequenceSchema + "." + table.SequenceName)}, {maxId}, true)";
                await setvalCommand.ExecuteScalarAsync().ConfigureAwait(false);
                return;
            }

            logger.LogDebug(
                "Sequence OK: {Schema}.{Table}.{Column} current={CurrentIdentity} maxId={MaxId}",
                table.SchemaName,
                table.TableName,
                table.ColumnName,
                currentIdentity,
                maxId);
        }

        #endregion

        #region SQL helpers

        private static (string Schema, string Name) SplitSequenceName(string qualified)
        {
            var trimmed = qualified.Trim('"');
            var parts = qualified.Split('.', 2);
            if (parts.Length == 2)
                return (parts[0].Trim('"'), parts[1].Trim('"'));
            return ("public", trimmed);
        }

        private static string QuoteIdentifier(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

        private static string QuoteQualifiedName(string schema, string table) =>
            $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";

        private static string QuoteLiteral(string value) =>
            $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

        #endregion

        #region Model

        private sealed class SequenceTableInfo
        {
            public string SchemaName { get; set; } = default!;
            public string TableName { get; set; } = default!;
            public string ColumnName { get; set; } = default!;
            public string SequenceSchema { get; set; } = default!;
            public string SequenceName { get; set; } = default!;
        }

        #endregion
    }
}
