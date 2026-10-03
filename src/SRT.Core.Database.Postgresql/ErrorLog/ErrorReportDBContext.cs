using Microsoft.EntityFrameworkCore;
using SRT.Core.Domain;
using SRT.Core.Database.Postgresql.ErrorLog.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SRT.Core.Database.Postgresql.ErrorLog
{
    public class ErrorReportDBContext : PostgreSqlDataBase
    {
        public DbSet<ErrorReport> ErrorReports { get; set; }

        #region Constructors
        public ErrorReportDBContext(DbContextOptions<ErrorReportDBContext> options,
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
                    dbName = "ErrorDatabase",
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
            ConnectionStrings.SRTCore_ErrorSQL.InheritConnectionTypeFrom(ConnectionStrings.DefaultConnection);
            builder.Services.AddDbContext<ErrorReportDBContext>(options =>
                options.UseNpgsql(
                    ConnectionStrings.SRTCore_ErrorSQL.GetConnectionString(),
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(ErrorReportDBContext).Assembly.GetName().Name);
                        npgsql.MigrationsHistoryTable("__EFMigrationsHistory_ErrorLog");
                    }));
        }

        public override Task InsertBaseData() => Task.CompletedTask;
        #endregion

        #region On Model Creating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ErrorReport>(entity =>
            {
                entity.ToTable("ErrorReports");
                entity.HasKey(e => e.id);

                entity.Property(e => e.category).HasMaxLength(200);
                entity.Property(e => e.serviceName).HasMaxLength(200);
                entity.Property(e => e.CorrelationId).HasMaxLength(100);
                entity.Property(e => e.Service).HasMaxLength(200);
                entity.Property(e => e.Environment).HasMaxLength(50);
                entity.Property(e => e.Version).HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Operation).HasMaxLength(100);
                entity.Property(e => e.Severity).HasMaxLength(20);
                entity.Property(e => e.Outcome).HasMaxLength(30);
                entity.Property(e => e.ExceptionType).HasMaxLength(200);
                entity.Property(e => e.Target).HasMaxLength(300);
                entity.Property(e => e.date).HasColumnType("timestamp without time zone");

                entity.HasIndex(e => e.ErrorId)
                    .IsUnique()
                    .HasFilter("\"ErrorId\" IS NOT NULL")
                    .HasDatabaseName("IX_ErrorReports_ErrorId");

                entity.HasIndex(e => new { e.Category, e.date })
                    .HasDatabaseName("IX_ErrorReports_Category_date");

                entity.HasIndex(e => e.CorrelationId)
                    .HasDatabaseName("IX_ErrorReports_CorrelationId");

                entity.HasIndex(e => e.Target)
                    .HasDatabaseName("IX_ErrorReports_Target");

                entity.HasIndex(e => e.serviceName)
                    .HasDatabaseName("IX_ErrorReports_serviceName");

                entity.HasIndex(e => e.category)
                    .HasDatabaseName("IX_ErrorReports_category");

                entity.HasIndex(e => e.serviceID)
                    .HasDatabaseName("IX_ErrorReports_serviceID");
            });
        }
        #endregion
    }
}
