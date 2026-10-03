using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SRT.Core.Domain;

namespace SRT.Core.Database.Postgresql.ErrorLog
{
    public sealed class ErrorReportDBContextFactory : IDesignTimeDbContextFactory<ErrorReportDBContext>
    {
        public ErrorReportDBContext CreateDbContext(string[] args)
        {
            var connection = new GeneralConnectionString
            {
                connectionType = "postgresql",
                dbName = "ErrorDatabase",
                host = "localhost",
                port = "5432",
                user = "postgres",
                pass = "postgres"
            };

            var options = new DbContextOptionsBuilder<ErrorReportDBContext>()
                .UseNpgsql(
                    connection.GetConnectionString(),
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(ErrorReportDBContext).Assembly.GetName().Name);
                        npgsql.MigrationsHistoryTable("__EFMigrationsHistory_ErrorLog");
                    })
                .Options;

            return new ErrorReportDBContext(options, new AppConnectionString { SRTCore_ErrorSQL = connection });
        }
    }
}
