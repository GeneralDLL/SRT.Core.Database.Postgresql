using SRT.Core.Database.Postgresql.Cache.Domains;
using SRT.Core.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SRT.Core.Database.Postgresql.Cache
{
    public class CacheDBContext : PostgreSqlDataBase
    {
        public DbSet<CashBackup> CashBackup { get; set; }

        #region Constructors
        public CacheDBContext(DbContextOptions<CacheDBContext> options,
                             AppConnectionString ConnectionStrings)
        : base(options, ConnectionStrings)
        {
        }
        #endregion

        #region On Configuration
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                Console.WriteLine("Base Configuration -------------------- Attention");

                var connection = new GeneralConnectionString()
                {
                    connectionType = "postgresql",
                    dbName = "CacheDatabase",
                    host = "localhost",
                    port = "5432",
                    user = "postgres",
                    pass = "postgres"
                };

                optionsBuilder.UseNpgsql(connection.GetConnectionString())
                              .LogTo(
                               Console.WriteLine,
                               new[] { DbLoggerCategory.Database.Command.Name },
                               Microsoft.Extensions.Logging.LogLevel.Information)
                              .EnableSensitiveDataLogging();
            }
        }
        #endregion

        #region Base Function
        public override void BaseConfiguration(WebApplicationBuilder builder, AppConnectionString ConnectionStrings)
        {
            ConnectionStrings.SRTCore_CacheSQL.InheritConnectionTypeFrom(ConnectionStrings.DefaultConnection);
            builder.Services.AddDbContext<CacheDBContext>(options =>
                options.UseNpgsql(
                    ConnectionStrings.SRTCore_CacheSQL.GetConnectionString(),
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(CacheDBContext).Assembly.GetName().Name);
                        npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Cache");
                    }));
        }

        public override async Task InsertBaseData()
        {
            await Database.ExecuteSqlRawAsync("""
                CREATE INDEX IF NOT EXISTS "IX_CashBackup_key_fts"
                ON "CashBackup"
                USING GIN (to_tsvector('simple', coalesce("key", '')));
                """);
        }
        #endregion

        #region On Model Creating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CashBackup>(entity =>
            {
                entity.HasKey(e => e.id);

                entity.Property(e => e.category).HasMaxLength(200);
                entity.Property(e => e.serviceName).HasMaxLength(200);
                entity.Property(e => e.key).HasMaxLength(450);
                entity.Property(e => e.value1).HasMaxLength(3990);
                entity.Property(e => e.value2).HasMaxLength(3990);
                entity.Property(e => e.dateCreate).HasColumnType("timestamp without time zone");
                entity.Property(e => e.dateExpire).HasColumnType("timestamp without time zone");

                entity.HasIndex(e => e.serviceID)
                    .HasDatabaseName("IX_CashBackup_serviceID");

                entity.HasIndex(e => e.serviceName)
                    .HasDatabaseName("IX_CashBackup_serviceName");

                entity.HasIndex(e => e.key)
                    .HasDatabaseName("IX_CashBackup_key");

                entity.HasIndex(e => new { e.serviceID, e.category, e.key })
                    .IsUnique()
                    .HasDatabaseName("UX_CashBackup_serviceID_category_key")
                    .HasFilter("\"category\" IS NOT NULL AND \"key\" IS NOT NULL");
            });
        }
        #endregion
    }
}
