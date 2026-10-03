# SRT.Core.Database.Postgresql — Documentation Index

Package: **SRT.Core.Database.Postgresql**  
Target: .NET 10 / EF Core Npgsql  
Reviewed against: PostgreSQL repo `c6baa23` plus this port, Core repo `9076228` plus store-routing helpers

All filenames in this folder start with `SRT.Core.Database.Postgresql.` so you can copy the whole set into any consumer project without colliding with other packages’ docs.

## Reading order

| Audience | Start here | Then |
|----------|------------|------|
| Humans | [SRT.Core.Database.Postgresql.HUMAN_GUIDE.md](SRT.Core.Database.Postgresql.HUMAN_GUIDE.md) | [COOKBOOK](SRT.Core.Database.Postgresql.COOKBOOK.md), [API_REFERENCE](SRT.Core.Database.Postgresql.API_REFERENCE.md) |
| AI (all tools) | [SRT.Core.Database.Postgresql.AI_RULES.md](SRT.Core.Database.Postgresql.AI_RULES.md) | [API_REFERENCE](SRT.Core.Database.Postgresql.API_REFERENCE.md), [COOKBOOK](SRT.Core.Database.Postgresql.COOKBOOK.md) |
| Codex | [SRT.Core.Database.Postgresql.AGENTS.md](SRT.Core.Database.Postgresql.AGENTS.md) | AI_RULES |
| Claude | [SRT.Core.Database.Postgresql.CLAUDE.md](SRT.Core.Database.Postgresql.CLAUDE.md) | AI_RULES |
| Cursor | Install [SRT.Core.Database.Postgresql.CURSOR_RULE.mdc](SRT.Core.Database.Postgresql.CURSOR_RULE.mdc) | AI_RULES |
| Copy paths per AI | [SRT.Core.Database.Postgresql.AI_INSTALL.md](SRT.Core.Database.Postgresql.AI_INSTALL.md) | — |

## Files in this set

1. `SRT.Core.Database.Postgresql.README.md` — this index
2. `SRT.Core.Database.Postgresql.HUMAN_GUIDE.md` — how the package works
3. `SRT.Core.Database.Postgresql.API_REFERENCE.md` — exact contracts
4. `SRT.Core.Database.Postgresql.COOKBOOK.md` — step-by-step recipes
5. `SRT.Core.Database.Postgresql.AI_RULES.md` — canonical MUST / MUST NOT
6. `SRT.Core.Database.Postgresql.AGENTS.md` — Codex entry
7. `SRT.Core.Database.Postgresql.CLAUDE.md` — Claude entry
8. `SRT.Core.Database.Postgresql.CURSOR_RULE.mdc` — Cursor rule **template**
9. `SRT.Core.Database.Postgresql.AI_INSTALL.md` — which files to copy where for Cursor / Claude / Codex

## Copy into a consumer project

After installing / referencing `SRT.Core.Database.Postgresql`, follow **[SRT.Core.Database.Postgresql.AI_INSTALL.md](SRT.Core.Database.Postgresql.AI_INSTALL.md)** for exact destinations per AI.

Short version:

1. Copy knowledge files into `<project>/docs/` (keep package-prefixed names).
2. Cursor: also copy `.mdc` → `<project>/.cursor/rules/`.
3. Claude / Codex: copy entry file to root or `docs/`, and optionally add a one-line pointer in your main `CLAUDE.md` / `AGENTS.md`.
4. At the start of an AI session, paste the bootstrap block below if the tool did not auto-load the entry file.

## Session bootstrap (paste for Cursor / Claude / Codex)

```text
You are working with SRT.Core.Database.Postgresql.
Before changing or consuming this package:
1) Read SRT.Core.Database.Postgresql.AI_RULES.md
2) Use SRT.Core.Database.Postgresql.API_REFERENCE.md for signatures/contracts
3) Follow SRT.Core.Database.Postgresql.COOKBOOK.md for new DbContext/Repo/UoW
Rules: read docs first; do not invent APIs; if ambiguous, inspect related code and update these docs.
Cache and ErrorLog inherit PostgreSqlDataBase but MUST NOT use BaseDBContextRepository / BaseDBContextUnitOfWork.
Postgres-only hosts use connectionType postgresql (or postgres/postgress). Cache is keyed CacheServiceKeys.Sql ("sql") even on PostgreSQL.
Do not project-reference SRT.Core.Database.SQLServer in a Postgres-only host.
```

## Install Cursor rule template

Files under package `docs/` are **not** auto-loaded by Cursor.

To activate in a consumer project:

1. Copy `SRT.Core.Database.Postgresql.CURSOR_RULE.mdc` to  
   `<consumer>/.cursor/rules/SRT.Core.Database.Postgresql.CURSOR_RULE.mdc`
2. Keep the package-prefixed filename.
3. Ensure the rule’s relative links (or the bootstrap paste) still point at the copied AI_RULES file.

## Loading note

Placing `AGENTS.md` / `CLAUDE.md` only under package `docs/` does **not** guarantee automatic load in parent or consumer repos. Use the bootstrap paste and/or copy the tool entry files to the locations your tool actually reads, **keeping the package prefix** in the filename when co-located with other package docs.
