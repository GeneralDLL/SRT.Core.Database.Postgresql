# SRT.Core.Database.Postgresql — API Reference

Reviewed: PostgreSQL repo `c6baa23` plus this port, Core repo `9076228` plus store-routing helpers, `PostgreSqlDataBase.RunIdentityGapFix => false`.  
Prefer this document for consumption. If docs disagree with code, inspect the related source and update this file.

---

## 1. Plugin — `PostgresqlBasicConfigurations`

- Namespace: `SRT.Core.Database.Postgresql`
- Implements: `ISRTBasicConfigurations`
- `Order` → `21`
- `FeatureName` → `SRTFeatureNames.PostgreSQL` (`"PostgreSQL"`)

### `SRT_ConfigBuilder(WebApplicationBuilder builder, AppSetting configApp)`

| Condition | Registration |
|-----------|--------------|
| `configApp.IsErrorLogPostgresStore()` | `SRT_AddGeneralConfig(..., ErrorReportDBContext.Namespace)` + `AddScoped<IErrorLogger, ErrorLogger>()` |
| `configApp.IsCachePostgresStore()` | Cache namespace config + `AddSingleton<ICacheSqlPersistence>` (`IsAvailable => true`) + `AddScoped<PostgresBackedCacheRepository>()` + `AddKeyedScoped<ICacheRepository>(CacheServiceKeys.Sql, …)` |
| `configApp.IsPostgresDatabaseRequested()` | For each distinct `lstDatabaseNamespace` entry: `SRT_AddGeneralConfig(connectionStrings, ns)` |

Loading the plugin does **not** auto-register Cache/ErrorLog; flags + store type must match.

### `SRT_ConfigApp(WebApplication app, AppSetting configApp)`

Builds distinct namespace list (app DBs + ErrorLog + Cache when those stores are Postgres), then `await app.SRT_CheckDatabase(ns)` for each.

---

## 2. Discovery / startup — `PostgresqlConfigurations`

Namespace: `SRT.Core.Database.Postgresql`

### Builder: `SRT_AddGeneralConfig`

- Discovers non-abstract `IDataBase` implementers under a namespace prefix (or marker type’s namespace)
- Keeps types assignable to `PostgreSqlDataBase`
- **Throws** `InvalidOperationException` if the same DbContext type is registered twice
- Instantiates `(DbContextOptions<T>, AppConnectionString)` and invokes `BaseConfiguration(builder, connectionStrings)`

### App: `SRT_CheckDatabase` (per `PostgreSqlDataBase` type, separate scopes)

Uses `GetRequiredService(contextType)` as `PostgreSqlDataBase`.

1. `MigrateAsync()` always; fail if pending remain
2. `InsertBaseData()`
3. `ApplyStartupDataMaintenanceAsync()`; read `RunIdentityGapFix`
4. If `RunIdentityGapFix`: `CheckSequenceGap.SRT_FixIdentityGapsAsync<TContext>(app)`
5. On failure: `DiagnosticReporter.ReportAsync` (`Category = SqlMigrate`, `PersistToErrorDb = true`) then rethrow

Migrate log target parses Npgsql `Host=` / `Database=` keys.

---

## 3. `PostgreSqlDataBase`

- Namespace: `SRT.Core.Database.Postgresql`
- File: `PostgreSqlDataBase.cs`
- Base: `DbContext`, implements `IDataBase`

| Member | Signature / default | Notes |
|--------|---------------------|-------|
| `ConnectionStrings` | `AppConnectionString` | Injected |
| `Provider` | `virtual DatabaseProvider Provider => PostgreSql` | |
| `RunIdentityGapFix` | `virtual bool => false` | Override `true` on domain contexts that need sequence maintenance |
| ctor | `(DbContextOptions options, AppConnectionString connectionStrings)` | `protected` |
| `BaseConfiguration` | `abstract void (WebApplicationBuilder, AppConnectionString)` | Register `AddDbContext` + `UseNpgsql` |
| `InsertBaseData` | `abstract Task` | Seed |
| `ApplyStartupDataMaintenanceAsync` | `virtual Task => CompletedTask` | Optional |

**Cache / ErrorLog:** `CacheDBContext` and `ErrorReportDBContext` inherit `PostgreSqlDataBase` and keep default `RunIdentityGapFix = false`. They do **not** use repository/UoW bases.

---

## 4. Core contracts (`SRT.Core`)

Same `IDataBase`, `IUnitOfWork`, `IRepository<TEntity,TKey>`, entity markers, `SearchInput` / `SearchResult<T>` / `PaginationHelper` (`MaxPageSize = 500`) as documented for SQL Server. `RunIdentityGapFix` is on `PostgreSqlDataBase` (and SQL `DataBase`), not on `IDataBase`.

---

## 5. `BaseDBContextRepository`

- Namespace: `SRT.Core.Database.Postgresql.RepositoriesBase`
- Holds `protected PostgreSqlDataBase DbContext` — **never dispose**

Behavior matches the SQL Server bases: `GetByIdAsync` (`AsNoTracking`); `AddOrUpdateAsync` (`autoSave` saves entire context); `AddOrUpdateRangeAsync` (duplicate assigned keys throw; default keys allowed); `DeleteByFilterAsync` immediate `ExecuteDelete`; `ClearChangeTracker` explicit only; `ChangeActivationAsync`.

Typed class: `BaseDBContextRepository<TEntity, TKey> : IRepository<TEntity, TKey>`.

---

## 6. `BaseDBContextUnitOfWork : IUnitOfWork`

- Namespace: `SRT.Core.Database.Postgresql.UnitOfWorkBase`
- Holds `protected PostgreSqlDataBase DbContext`

| Method | Behavior |
|--------|----------|
| `SaveChangesAsync(ct)` | Context save |
| `SRT_BeginTransactionAsync` / `Commit` / `Rollback` | Prefer `ExecuteInTransactionAsync` |
| `ExecuteInTransactionAsync` (+ `TResult`) | `CreateExecutionStrategy().ExecuteAsync` → begin → func → **SaveChanges** → commit; catch → rollback + throw |
| `SRT_OrderBy` | Default field `id`; whitelist if `allowedFields` non-null; ThenBy `id` |
| `SRT_GenerateSearchResult` | include? → Count → Order(+PK) → Validate → page → map |
| `Filter<T>` | optional `isActive`, `date_Create >= date_Start`, `date_Create < date_End` |

---

## 7. Sequence maintenance — `CheckSequenceGap`

- Namespace: `SRT.Core.Database.Postgresql.UnitOfWorks` (`internal`)
- Entry: `SRT_FixIdentityGapsAsync<AppDbContext>(WebApplication app)` where `AppDbContext : DbContext`

| Rule | Behavior |
|------|----------|
| Source | `pg_get_serial_sequence`; skip catalogs / `__EFMigrationsHistory*` |
| `current > maxId` | Log only — **no** downward `setval` |
| `increment > 0` and `current < maxId` | `setval(seq, maxId, true)` |
| Per-table errors | Logged; loop continues |

---

## 8. Cache

Use **`ICacheRepository`** / **`ICacheSqlPersistence`**. Hosts that also use Redis inject **unkeyed** `ICacheRepository`. Do not inject `CacheDBContext` in application code.

| Type | Namespace | Role |
|------|-----------|------|
| `CacheDBContext` | `SRT.Core.Database.Postgresql.Cache` | `: PostgreSqlDataBase`; connection `SRTCore_CacheSQL`; history `__EFMigrationsHistory_Cache` |
| `CashBackup` | `...Cache.Domains` | Durable row |
| `PostgresBackedCacheRepository` | `...Cache` | `ICacheRepository`; ctor `(CacheDBContext, AppSetting)` |
| `CacheServiceKeys.Sql` | `SRT.Core.Interfaces.Cache` | `"sql"` — keyed registration (not a connection provider) |
| `CacheDBContextFactory` | design-time EF | |

`PostgresBackedCacheRepository`: lookup/upsert/remove/`ClearCategory` by `ServiceId`; expire `ExecuteDelete`; counters throw `NotSupportedException`.

`InsertBaseData` creates GIN index `IX_CashBackup_key_fts` on `to_tsvector('simple', coalesce("key", ''))`.

---

## 9. ErrorLog

Use **`IErrorLogger`** (`SRT.Core.Diagnostics`). Same method surface as SQL Server `ErrorLogger`.

| Rule | Detail |
|------|--------|
| Empty filter | At least one non-paging criterion or `ArgumentException` |
| Search Take | Default 50; max **200** |
| MessageContains | Concatenated Message1..Message5 |
| Delete | Same filter; `ExecuteDelete` |

`ErrorReportDBContext` uses `SRTCore_ErrorSQL` and history `__EFMigrationsHistory_ErrorLog`. Unique `ErrorId` filter is `"ErrorId" IS NOT NULL`.

---

## 10. AppSetting helpers (Core)

- `UseErrorLogSQL` default `true`; `UseCashStructure` default `CashWithSQL`
- `IsPostgresDatabaseRequested()` / `IsSqlDatabaseRequested()`
- `IsErrorLogPostgresStore()` / `IsErrorLogSqlStore()` / `IsCachePostgresStore()` / `IsCacheSqlStore()`
- `ResolveRelationalStoreType(store)` — explicit type wins; empty inherits `DefaultConnection`
- `IsPostgresConnectionType` — `postgress` \| `postgres` \| `postgresql`
- `IsSqlServerConnectionType` — empty \| `sql` \| `sqlserver`
- `GeneralConnectionString.GetConnectionString()` maps `postgres` / `postgresql` / `postgress` to Npgsql; `sql` / `sqlserver` to SQL Server
- `InheritConnectionTypeFrom` copies Default type onto empty Error/Cache types after config bind

`GetRequestedFeatures()` adds `PostgreSQL` if app DBs are Postgres **or** Error/Cache flags need a Postgres store; adds `SQLServer` only for SQL stores.

Do not register both plugins’ ErrorLog or keyed `"sql"` cache in one host.
