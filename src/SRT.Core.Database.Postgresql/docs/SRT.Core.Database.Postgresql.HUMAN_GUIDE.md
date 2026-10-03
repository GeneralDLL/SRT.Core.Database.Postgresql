# SRT.Core.Database.Postgresql — Human Guide

This guide explains how the PostgreSQL package fits into an SRT host, what it registers, and how you add your own databases. For exact method signatures see [SRT.Core.Database.Postgresql.API_REFERENCE.md](SRT.Core.Database.Postgresql.API_REFERENCE.md). For copy-paste recipes see [SRT.Core.Database.Postgresql.COOKBOOK.md](SRT.Core.Database.Postgresql.COOKBOOK.md).

## What this package is

`SRT.Core.Database.Postgresql` is an SRT feature plugin (`PostgresqlBasicConfigurations`, `Order = 21`, feature name `"PostgreSQL"`). It is the **Npgsql substitute** for `SRT.Core.Database.SQLServer`: app DbContexts, durable cache backup, and ErrorLog.

When the host loads it, the plugin can:

- Register **ErrorLog** storage (`ErrorReportDBContext` + `IErrorLogger`) when `UseErrorLogSQL` is on **and** the Error store `connectionType` is PostgreSQL
- Register **RDBMS-backed cache** (`CacheDBContext` + keyed `ICacheRepository` `"sql"`) when `CashWithSQL` is on **and** the Cache store type is PostgreSQL
- Register **your app DbContexts** discovered by namespace (`lstDatabaseNamespace`) when DefaultConnection is PostgreSQL
- On startup: **migrate → seed → optional data maintenance → optional sequence fix**

It depends on `SRT.Core`. This package does **not** reference Redis or SQL Server. Dual-store order lives in `SRT.Core.Database.Redis` (`RedisSqlCacheRepository`).

A Postgres-only host ProjectReferences **this** package, not `SRT.Core.Database.SQLServer`. Core routes `UseErrorLogSQL` / `CashWithSQL` to PostgreSQL vs SQL Server from `connectionType`.

## How it plugs into a host

Every SRT web host uses a thin `Program.cs`:

```csharp
var dmAppSetting = args.SRT_ConfigController();
var (builder, config) = StartAppConfigurations.SRT_ConfigController(args, dmAppSetting);
var app = await builder.SRT_ConfigBuilder(config)
    .SRT_ConfigBuilderAndBuildApp(config)
    .SRT_ConfigApp(config);
app.Run();
```

- Local DI / Swagger / middleware live in the app’s `BasicProjectConfig`, not in `Program.cs`.
- `UseAuthorization` and `MapControllers` are owned by Core — do not repeat them in the host.
- The plugin is discovered because the host **project-references** this package.

## Feature flags and store routing

Core helpers on `AppSetting`:

| Helper | Effect |
|--------|--------|
| `IsPostgresDatabaseRequested()` | `lstDatabaseNamespace` non-empty **and** DefaultConnection type is `postgress` / `postgres` / `postgresql` |
| `IsErrorLogPostgresStore()` | `UseErrorLogSQL` and ErrorSQL type (or inherited Default) is PostgreSQL |
| `IsCachePostgresStore()` | `CashWithSQL` and CacheSQL type (or inherited Default) is PostgreSQL |
| Matching `Is*SqlStore` / `IsSqlDatabaseRequested` | SQL Server plugin only |

Empty `SRTCore_ErrorSQL` / `SRTCore_CacheSQL` `connectionType` inherits `DefaultConnection` (config bind + `InheritConnectionTypeFrom`).

Connection slots:

| Feature | Connection property |
|---------|---------------------|
| App DBs | `ConnectionStrings.DefaultConnection` |
| ErrorLog | `ConnectionStrings.SRTCore_ErrorSQL` |
| Durable cache | `ConnectionStrings.SRTCore_CacheSQL` |

`CacheServiceKeys.Sql` (`"sql"`) is the Redis dual-store **key name** for durable RDBMS backup. It does **not** force a SQL Server connection string.

Service identity comes from Core `BindServiceIdentity`: `Service:Id` / `Service:Name`. Unique `Id` per microservice.

## Startup pipeline (per discovered `PostgreSqlDataBase`)

1. **Migrate** — always `MigrateAsync()`; fails if migrations remain pending
2. **`InsertBaseData()`**
3. **`ApplyStartupDataMaintenanceAsync()`**
4. **Sequence gap fix** — only if `RunIdentityGapFix == true`

Default: `RunIdentityGapFix => false`. Cache and ErrorLog leave it false.

Sequence rules when enabled:

- Serial/identity sequences via `pg_get_serial_sequence` / `pg_sequences`; skip `__EFMigrationsHistory*`
- If sequence is **ahead** of `MAX(id)` → **log only** (never `setval` downward)
- If sequence is **behind** `MAX(id)` and increment is positive → `setval` **upward** only (`is_called = true` so next ≈ maxId + increment)
- Uniqueness, not gapless numbering

## Architecture layers

```text
Host AppSetting / BasicProjectConfig
        │
        ▼
PostgresqlBasicConfigurations  (plugin)
        │
        ├── ErrorReportDBContext + ErrorLogger     (direct DbContext; not Repo/UoW bases)
        ├── CacheDBContext + PostgresBackedCacheRepository as keyed ICacheRepository "sql"
        └── Your AppDbContext : PostgreSqlDataBase
                ├── YourRepository : BaseDBContextRepository<TEntity,TKey>
                └── YourUnitOfWork : BaseDBContextUnitOfWork
```

### PostgreSQL bases (this package)

- `PostgreSqlDataBase` — abstract `DbContext` + `IDataBase`
- `BaseDBContextRepository` / `BaseDBContextRepository<TEntity,TKey>`
- `BaseDBContextUnitOfWork`

Repositories under one UoW must share the **same** context instance and must **not** dispose it.

## Cache subsystem (do not use IRepository)

When `IsCachePostgresStore()`:

- `PostgresBackedCacheRepository` is RDBMS-only (`CashBackup`). Ctor: `(CacheDBContext, AppSetting)`.
- Registered as scoped concrete type **and** keyed `ICacheRepository` with `CacheServiceKeys.Sql`. Unkeyed `ICacheRepository` is Redis.
- Counters / rate-limit / backoff throw `NotSupportedException`.
- `InsertBaseData` creates a GIN `to_tsvector` index on `key` (not SQL Server FTS).
- Application code injects unkeyed `ICacheRepository`, not `CacheDBContext`.

## ErrorLog subsystem (do not use IRepository)

When `IsErrorLogPostgresStore()`:

- `ErrorLogger` implements `IErrorLogger`
- Search/delete: at least one non-paging criterion; Take default 50, max 200
- `MessageContains` searches concatenated Message1..Message5
- Persist logs via Serilog first; DB write is best-effort with a short timeout

## Adding your own database (summary)

1. Put a `DbContext : PostgreSqlDataBase` in a namespace listed in `lstDatabaseNamespace`
2. Implement `BaseConfiguration` (`AddDbContext` + `UseNpgsql` + migrations assembly), `InsertBaseData`
3. Optionally override `RunIdentityGapFix => true`
4. Entities with `IDatabaseEntry` + `IDatabaseEntryID`
5. Repos from `BaseDBContextRepository<TEntity,TKey>` and UoW from `BaseDBContextUnitOfWork`, same context
6. Register as scoped in local `SRT_ConfigBuilder`
7. Design-time factory if you use EF tools

Details: [SRT.Core.Database.Postgresql.COOKBOOK.md](SRT.Core.Database.Postgresql.COOKBOOK.md).

## Mental model for saves and deletes

| Operation | Behavior |
|-----------|----------|
| `AddOrUpdateAsync(..., autoSave: true)` | Saves **all pending changes** on the shared context |
| `AddOrUpdateRangeAsync` | Stages with `autoSave: false`, then **one** Save |
| `DeleteByFilterAsync` | Immediate `ExecuteDelete` |
| `ExecuteInTransactionAsync` | action → `SaveChangesAsync` → commit; on failure rollback + rethrow |
