# SRT.Core.Database.Postgresql — AI Rules (canonical)

These rules are mandatory for Cursor, Claude, Codex, and any other agent working in a repo that consumes or modifies **SRT.Core.Database.Postgresql**.

Companion docs (same folder, package-prefixed names):

- [SRT.Core.Database.Postgresql.API_REFERENCE.md](SRT.Core.Database.Postgresql.API_REFERENCE.md)
- [SRT.Core.Database.Postgresql.COOKBOOK.md](SRT.Core.Database.Postgresql.COOKBOOK.md)
- [SRT.Core.Database.Postgresql.HUMAN_GUIDE.md](SRT.Core.Database.Postgresql.HUMAN_GUIDE.md)

## Working rule

1. **Read these docs first** before implementing features that use this package.
2. **Do not invent APIs** that duplicate `PostgreSqlDataBase`, `BaseDBContextRepository`, `BaseDBContextUnitOfWork`, `IRepository`, or `IUnitOfWork`.
3. If docs are ambiguous or wrong: **inspect the related code** and **update these docs** in the same change when possible.
4. Normal consumption should be possible from docs alone; package internals still require code review when changing the package itself.

## Host / plugin

MUST:

- Keep `Program.cs` thin (`SRT_ConfigController` → `SRT_ConfigBuilder` → `SRT_ConfigBuilderAndBuildApp` → `SRT_ConfigApp`).
- Put local DI in the app’s `BasicProjectConfig.SRT_ConfigBuilder`.
- Project-reference this package so `PostgresqlBasicConfigurations` is discovered.
- For a **Postgres-only** host, do **not** ProjectReference `SRT.Core.Database.SQLServer`.
- List each app DbContext namespace in `AppSetting.lstDatabaseNamespace`.
- Set `connectionType` to `postgresql`, `postgres`, or `postgress` on `DefaultConnection` (and on `SRTCore_ErrorSQL` / `SRTCore_CacheSQL` when those flags are on). Empty Error/Cache types inherit `DefaultConnection`.
- Configure connection slots: `DefaultConnection`, `SRTCore_ErrorSQL`, `SRTCore_CacheSQL` as needed. `GetConnectionString()` uses `connectionType`, not `CacheServiceKeys.Sql`.
- Bind `Service.Id` / `Service.Name` in every environment appsettings file (unique `Id` per microservice).

MUST NOT:

- Call `UseAuthorization` or `MapControllers` in the host (Core owns them).
- Register the same `PostgreSqlDataBase` / DbContext type twice.
- Put Swagger/PostgreSQL DI/middleware in `Program.cs`.
- Register both SQL Server and PostgreSQL `IErrorLogger` or two keyed `"sql"` cache repositories in one host.

## DataBase / startup

MUST:

- Inherit app contexts from `SRT.Core.Database.Postgresql.PostgreSqlDataBase`.
- Implement `BaseConfiguration` with `UseNpgsql`, `InsertBaseData`.
- Treat startup order as: migrate → `InsertBaseData` → `ApplyStartupDataMaintenanceAsync` → sequence fix **only if** `RunIdentityGapFix`.
- Keep default `RunIdentityGapFix => false`; override `true` only on domain contexts that need it.
- Give Cache and ErrorLog their own EF history tables when they might share a database (`__EFMigrationsHistory_Cache`, `__EFMigrationsHistory_ErrorLog`).

MUST NOT:

- Assume Cache/ErrorLog run sequence maintenance (they keep `RunIdentityGapFix = false`).
- `setval` **downward** at startup (package logs ahead-of-data gaps only).
- Treat sequence fix as gapless numbering (rollbacks still create gaps).
- Call SQL Server catalog objects (`sys.*`, `DBCC CHECKIDENT`, `FULLTEXTSERVICEPROPERTY`) from this package.

## Repository / Unit of work

MUST:

- Share **one** `PostgreSqlDataBase` instance across all repositories in a UoW (scoped DI).
- Inherit typed repos from `BaseDBContextRepository<TEntity,TKey>` (or non-generic helpers when multi-entity).
- Inherit UoW from `BaseDBContextUnitOfWork` and implement `IUnitOfWork`.
- Treat `autoSave: true` as saving **all pending changes** on the shared context.
- Use `AddOrUpdateRangeAsync` for batches (one Save at end).
- Treat `DeleteByFilterAsync` as **immediate** `ExecuteDelete`.
- Combine bulk delete/update with tracked changes **only** inside `ExecuteInTransactionAsync`.
- Pass an **order-field whitelist** to `SRT_OrderBy` / `SRT_GenerateSearchResult` for user input.
- Apply business filters before `SRT_GenerateSearchResult` (pipeline: include? → count → order(+PK) → validate page → page).
- Respect `PaginationHelper.MaxPageSize` (`500`).
- Use `CancellationToken` on I/O methods.

MUST NOT:

- Dispose `DbContext` / `PostgreSqlDataBase` inside a repository or UoW.
- Call `ClearChangeTracker` automatically between operations (caller-only).
- Use empty catch blocks around EF `Update`.
- Treat non-default keys (especially client `Guid`) as “exists” without a DB existence check.
- Allow duplicate **assigned** keys in one range (throw); multiple default keys (e.g. `id == 0`) are allowed.
- Put EF types (`IDbContextTransaction`, ExecuteUpdate setters) into Core interfaces.
- Invent a parallel CRUD/search stack when bases exist.

## Cache

MUST:

- Use `ICacheRepository` / `ICacheSqlPersistence` for cache features.
- Keep `PostgresBackedCacheRepository` as the durable store and register it as keyed `ICacheRepository` (`CacheServiceKeys.Sql`). Unkeyed `ICacheRepository` belongs to Redis when Redis is referenced.
- Persist `CashBackup.serviceID` / `serviceName` from `AppSetting`.
- Dual-store order (Redis then RDBMS) lives in Redis `RedisSqlCacheRepository`.

MUST NOT:

- Register unkeyed `AddScoped<ICacheRepository, PostgresBackedCacheRepository>()`.
- Treat `CacheServiceKeys.Sql` as a SQL Server connection selector.
- Project-reference Redis from this package for cache dual-store.
- Implement cache persistence through `IRepository` / `BaseDBContextRepository` / `BaseDBContextUnitOfWork`.
- Dispose `CacheDBContext` from the cache repository.
- Inject `CacheDBContext` in application controllers/services (inject unkeyed `ICacheRepository`).

## ErrorLog

MUST:

- Use `IErrorLogger` with `ErrorReportFilter` / `ErrorReportSummary`.
- Require at least one non-paging filter criterion for search/delete.
- Keep MessageContains on the full concatenated message.

MUST NOT:

- Route ErrorReport through app `IRepository` / UoW bases.
- Invent a second error-store API next to `IErrorLogger`.

## Documentation maintenance

MUST:

- Keep filenames prefixed with `SRT.Core.Database.Postgresql.` when copying into consumer projects.
- Update `SRT.Core.Database.Postgresql.API_REFERENCE.md` when public contracts change.
- Keep tool entry files (`AGENTS` / `CLAUDE` / `CURSOR_RULE`) as short pointers to this file.

MUST NOT:

- Rename these docs to generic `README.md` / `AGENTS.md` / `CLAUDE.md` when multiple package doc sets coexist in one consumer repo.
