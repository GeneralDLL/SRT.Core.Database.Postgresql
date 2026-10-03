using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SRT.Core.Domain;

namespace SRT.Core.Database.Postgresql.Cache
{
    public sealed class CacheDBContextFactory : IDesignTimeDbContextFactory<CacheDBContext>
    {
        public CacheDBContext CreateDbContext(string[] args)
        {
            var connection = new GeneralConnectionString
            {
                connectionType = "postgresql",
                dbName = "CacheDatabase",
                host = "localhost",
                port = "5432",
                user = "postgres",
                pass = "postgres"
            };

            var options = new DbContextOptionsBuilder<CacheDBContext>()
                .UseNpgsql(
                    connection.GetConnectionString(),
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(CacheDBContext).Assembly.GetName().Name);
                        npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Cache");
                    })
                .Options;

            return new CacheDBContext(options, new AppConnectionString { SRTCore_CacheSQL = connection });
        }
    }
}
