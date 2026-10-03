# SRT.Core.Database.Postgresql — Cookbook

Recipes for consuming this package. Signatures match [SRT.Core.Database.Postgresql.API_REFERENCE.md](SRT.Core.Database.Postgresql.API_REFERENCE.md). Replace `YourApp.*` names with your project’s.

---

## 1. Postgres-only host startup (self-contained)

Reference **`SRT.Core.Database.Postgresql`**, not `SRT.Core.Database.SQLServer`. Keep the same Core flags (`UseErrorLogSQL`, `CashType.CashWithSQL`); routing uses `connectionType`.

### `Program.cs` (web host — keep thin)

```csharp
var dmAppSetting = args.SRT_ConfigController();
var (builder, config) = StartAppConfigurations.SRT_ConfigController(args, dmAppSetting);
var app = await builder.SRT_ConfigBuilder(config)
    .SRT_ConfigBuilderAndBuildApp(config)
    .SRT_ConfigApp(config);
app.Run();
```

Do **not** call `UseAuthorization` / `MapControllers` here. Do **not** register PostgreSQL DI in `Program.cs`.

### `BasicProjectConfig`

```csharp
public static class BasicProjectConfig
{
    public static MyAppSetting SRT_ConfigController(this string[] args)
    {
        return new MyAppSetting
        {
            lstDatabaseNamespace = { "YourApp.Database" },

            UseErrorLogSQL = true,
            UseCashStructure = CashType.CashWithSQL,

            ConnectionStrings = new AppConnectionString
            {
                DefaultConnection = new GeneralConnectionString
                {
                    connectionType = "postgresql",
                    host = "localhost",
                    port = "5432",
                    dbName = "YourApp",
                    user = "postgres",
                    pass = "postgres",
                },
                SRTCore_ErrorSQL = new GeneralConnectionString
                {
                    connectionType = "postgresql",
                    host = "localhost",
                    port = "5432",
                    dbName = "ErrorDatabase",
                    user = "postgres",
                    pass = "postgres",
                },
                SRTCore_CacheSQL = new GeneralConnectionString
                {
                    connectionType = "postgresql",
                    host = "localhost",
                    port = "5432",
                    dbName = "CacheDatabase",
                    user = "postgres",
                    pass = "postgres",
                },
            },
        };
    }

    public static WebApplicationBuilder SRT_ConfigBuilder(this WebApplicationBuilder builder, MyAppSetting config)
    {
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IProductUnitOfWork, ProductUnitOfWork>();
        return builder;
    }

    public static async Task<WebApplication> SRT_ConfigApp(this Task<WebApplication> appTask, MyAppSetting config)
    {
        var app = await appTask;
        return app.SRT_ConfigApp(config);
    }

    public static WebApplication SRT_ConfigApp(this WebApplication app, MyAppSetting config)
    {
        return app;
    }
}
```

Accepted `connectionType` tokens: `postgresql`, `postgres`, `postgress`. If Error/Cache types are empty, they inherit `DefaultConnection`.

Add `Service` to **every** environment appsettings file:

```json
"Service": {
  "Id": 9001,
  "Name": "YourApp.Web"
}
```

---

## 2. App DbContext

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SRT.Core.Database.Postgresql;
using SRT.Core.Domain;

namespace YourApp.Database;

public sealed class AppDbContext : PostgreSqlDataBase
{
    public DbSet<Product> Products => Set<Product>();

    public override bool RunIdentityGapFix => true;

    public AppDbContext(DbContextOptions<AppDbContext> options, AppConnectionString connectionStrings)
        : base(options, connectionStrings)
    {
    }

    public override void BaseConfiguration(WebApplicationBuilder builder, AppConnectionString connectionStrings)
    {
        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(
                connectionStrings.DefaultConnection.GetConnectionString(),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));
    }

    public override Task InsertBaseData() => Task.CompletedTask;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.id);
            e.Property(x => x.name).HasMaxLength(200).IsRequired();
        });
        base.OnModelCreating(modelBuilder);
    }
}
```

### Design-time factory (EF tools)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SRT.Core.Domain;

namespace YourApp.Database;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = new AppConnectionString
        {
            DefaultConnection = new GeneralConnectionString
            {
                connectionType = "postgresql",
                host = "localhost",
                port = "5432",
                dbName = "YourApp",
                user = "postgres",
                pass = "postgres",
            }
        };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs.DefaultConnection.GetConnectionString())
            .Options;
        return new AppDbContext(options, cs);
    }
}
```

---

## 3. Entity markers

Same as SQL Server: `IDatabaseEntry`, `IDatabaseEntryID` / `IDatabaseEntryID<TKey>`, `IDatabaseEntryActivity`, `IDatabaseEntryAdmin`.

---

## 4. Repository

```csharp
using SRT.Core.Database.Postgresql.RepositoriesBase;
using SRT.Core.Interfaces.Databases;

namespace YourApp.Database;

public interface IProductRepository : IRepository<Product, int>
{
}

public sealed class ProductRepository : BaseDBContextRepository<Product, int>, IProductRepository
{
    public ProductRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}
```

---

## 5. Unit of work (shared context)

```csharp
using SRT.Core.Database.Postgresql.UnitOfWorkBase;
using SRT.Core.Domain.BaseDomains;
using SRT.Core.Interfaces.Databases;

namespace YourApp.Database;

public sealed class ProductUnitOfWork : BaseDBContextUnitOfWork, IProductUnitOfWork
{
    public IProductRepository Products { get; }

    public ProductUnitOfWork(AppDbContext dbContext, IProductRepository products) : base(dbContext)
    {
        Products = products;
    }

    private static readonly string[] AllowedOrderFields = { "id", "name", "date_Create" };

    public async Task<ProductSearchResult> SearchAsync(ProductSearchInput input, CancellationToken ct = default)
    {
        IQueryable<Product> query = DbContext.Set<Product>().AsNoTracking();
        query = Filter(query, input);

        var result = new ProductSearchResult(input);
        return await SRT_GenerateSearchResult(
            query,
            input,
            result,
            p => new ProductDto { Id = p.id, Name = p.name },
            AddInclude: null,
            allowedOrderFields: AllowedOrderFields,
            cancellationToken: ct);
    }
}
```

Register both as **scoped**.

---

## 6. Transactions

```csharp
await uow.ExecuteInTransactionAsync(async () =>
{
    await uow.Products.AddOrUpdateAsync(entity, admin, autoSave: false);
}, ct);
```

`DeleteByFilterAsync` / `ExecuteDelete` run immediately. Combine with tracked changes only inside `ExecuteInTransactionAsync`.

---

## 7. Sequence gap fix opt-in

```csharp
public override bool RunIdentityGapFix => true; // on your PostgreSqlDataBase subclass only
```

Leave `false` (default) for Cache/ErrorLog. Startup never `setval` **downward**.

---

## 8. Cache & ErrorLog as a consumer

### Cache

```csharp
await cache.AddNewData("cat", "key", value, expireUtc);
var item = await cache.GetData<MyDto>("cat", "key");
await cache.ClearCategory("cat");
```

Durable store is keyed `CacheServiceKeys.Sql`. For Redis+Postgres dual-store, also reference `SRT.Core.Database.Redis` with `CashType.CashWithSQL` and postgres `SRTCore_CacheSQL.connectionType`.

### ErrorLog

```csharp
await errorLogger.Save_Error_ToDB(ex);

var rows = await errorLogger.SearchByFilterAsync(new ErrorReportFilter
{
    ServiceName = "MyService",
    MessageContains = "timeout",
    Take = 50,
}, ct);
```

At least one non-paging filter field is required.

---

## 9. Checklist for a Postgres-only app

1. ProjectReference `SRT.Core.Database.Postgresql` (not SQL Server)
2. Set `connectionType` to `postgresql` on Default / Error / Cache slots
3. Set `lstDatabaseNamespace` + `PostgreSqlDataBase` subclass with `UseNpgsql`
4. Entities implement marker interfaces
5. Scoped Repo + UoW sharing the same context
6. User search: whitelist order fields
7. Bulk + tracked: only inside `ExecuteInTransactionAsync`
8. Copy this `docs/` set (prefixed filenames) into the consumer project
