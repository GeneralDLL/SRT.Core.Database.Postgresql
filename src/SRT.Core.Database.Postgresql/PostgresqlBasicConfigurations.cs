using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SRT.Core.Database.Postgresql.ErrorLog;
using SRT.Core.Database.Postgresql.ErrorLog.Repository;
using PgCache = SRT.Core.Database.Postgresql.Cache;
using SRT.Core.Domain;
using SRT.Core.Interfaces;
using SRT.Core.Diagnostics;
using SRT.Core.SystemEnums;
using SRT.Core.Interfaces.Cache;

namespace SRT.Core.Database.Postgresql
{
    /// <summary>
    /// Registers PostgreSQL feature services (ErrorLog, Cache, app DbContexts) and runs
    /// migrate / seed / optional sequence maintenance at app startup.
    /// Cache/ErrorLog do not use Repo/UoW bases.
    /// </summary>
    public class PostgresqlBasicConfigurations : ISRTBasicConfigurations
    {
        public int Order => 21;
        public string FeatureName => SRTFeatureNames.PostgreSQL;

        #region Builder

        public Task<WebApplicationBuilder> SRT_ConfigBuilder(WebApplicationBuilder builder, AppSetting configApp)
        {
            #region ErrorLog PostgreSQL
            if (configApp.IsErrorLogPostgresStore())
            {
                builder.SRT_AddGeneralConfig(configApp.ConnectionStrings, typeof(ErrorReportDBContext).Namespace!);
                builder.Services.AddScoped<IErrorLogger, ErrorLogger>();
            }
            #endregion

            #region Cache PostgreSQL
            if (configApp.IsCachePostgresStore())
            {
                builder.SRT_AddGeneralConfig(configApp.ConnectionStrings, typeof(PgCache.CacheDBContext).Namespace!);
                builder.Services.AddSingleton<ICacheSqlPersistence>(_ => new CacheSqlPersistence());
                builder.Services.AddScoped<PgCache.PostgresBackedCacheRepository>();
                builder.Services.AddKeyedScoped<ICacheRepository>(
                    CacheServiceKeys.Sql,
                    (sp, _) => sp.GetRequiredService<PgCache.PostgresBackedCacheRepository>());
            }
            #endregion

            #region App database namespaces
            if (configApp.IsPostgresDatabaseRequested())
            {
                foreach (var ns in configApp.lstDatabaseNamespace.Distinct())
                    builder.SRT_AddGeneralConfig(configApp.ConnectionStrings, ns);
            }
            #endregion

            return Task.FromResult(builder);
        }

        #endregion

        #region App

        public async Task<WebApplication> SRT_ConfigApp(WebApplication app, AppSetting configApp)
        {
            var namespaces = new List<string>();

            if (configApp.IsPostgresDatabaseRequested())
                namespaces.AddRange(configApp.lstDatabaseNamespace);

            if (configApp.IsErrorLogPostgresStore())
                namespaces.Add(typeof(ErrorReportDBContext).Namespace!);

            if (configApp.IsCachePostgresStore())
                namespaces.Add(typeof(PgCache.CacheDBContext).Namespace!);

            foreach (var ns in namespaces.Distinct())
                await app.SRT_CheckDatabase(ns);

            return app;
        }

        #endregion

        #region Nested

        private sealed class CacheSqlPersistence : ICacheSqlPersistence
        {
            public bool IsAvailable => true;
        }

        #endregion
    }
}
