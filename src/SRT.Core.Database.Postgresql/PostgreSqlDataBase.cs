using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using SRT.Core.Domain;
using SRT.Core.Interfaces.Databases;

namespace SRT.Core.Database.Postgresql
{
    public abstract class PostgreSqlDataBase : DbContext, IDataBase
    {
        public AppConnectionString ConnectionStrings { get; }

        public virtual DatabaseProvider Provider => DatabaseProvider.PostgreSql;

        /// <summary>
        /// When true, startup runs sequence maintenance (upward setval only).
        /// Default false; domain contexts that need it must override to true.
        /// Cache/ErrorLog contexts leave this false.
        /// </summary>
        public virtual bool RunIdentityGapFix => false;

        protected PostgreSqlDataBase(DbContextOptions options, AppConnectionString connectionStrings) : base(options)
        {
            ConnectionStrings = connectionStrings;
        }

        public abstract void BaseConfiguration(WebApplicationBuilder builder, AppConnectionString ConnectionStrings);

        public abstract Task InsertBaseData();

        public virtual Task ApplyStartupDataMaintenanceAsync() => Task.CompletedTask;
    }
}
