# SRT.Core.Database.Postgresql — Claude entry

Package: **SRT.Core.Database.Postgresql**

## Required reading

- [SRT.Core.Database.Postgresql.AI_RULES.md](SRT.Core.Database.Postgresql.AI_RULES.md) — follow entirely
- [SRT.Core.Database.Postgresql.API_REFERENCE.md](SRT.Core.Database.Postgresql.API_REFERENCE.md) — signatures and contracts
- [SRT.Core.Database.Postgresql.COOKBOOK.md](SRT.Core.Database.Postgresql.COOKBOOK.md) — implementation recipes

## Working rule

Read docs first. Do not invent APIs. If ambiguous, inspect related code and update these docs.

Cache and ErrorLog inherit `PostgreSqlDataBase` but must not use `BaseDBContextRepository` / `BaseDBContextUnitOfWork`. Durable cache is keyed `CacheServiceKeys.Sql` (`"sql"`); unkeyed `ICacheRepository` is Redis. Postgres-only hosts must not reference `SRT.Core.Database.SQLServer`.

Keep the `SRT.Core.Database.Postgresql.` filename prefix when copying into consumer projects. This entry file is a pointer only; `AI_RULES.md` is authoritative.
