using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using SG01G02_MVC.Infrastructure.Data;

namespace SG01G02_MVC.Infrastructure.Configuration;

public class DatabaseConfigurator
{
    public void Configure(WebApplicationBuilder builder)
    {
        if (ShouldUseInMemoryDatabase(builder))
        {
            ConfigureInMemoryDatabase(builder);
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            ConfigureSqliteDatabase(builder);
            return;
        }

        ConfigurePostgreSqlDatabase(builder);
    }

    public void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Database provider: {Provider}", db.Database.ProviderName);

            if (IsInMemoryDatabase(db))
            {
                logger.LogInformation("Using in-memory database - no migrations needed");
            }
            else if (IsSqliteDatabase(db))
            {
                logger.LogInformation("Ensuring SQLite database is created");
                db.Database.EnsureCreated();
            }
            else if (IsPostgreSqlDatabase(db))
            {
                HandlePostgreSqlMigrations(db, logger);
            }
            else
            {
                logger.LogWarning("Unknown database provider: {Provider}. Attempting EnsureCreated as fallback",
                    db.Database.ProviderName);
                db.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database initialization");
        }
    }

    private bool ShouldUseInMemoryDatabase(WebApplicationBuilder builder)
    {
        return builder.Environment.IsEnvironment("Testing") ||
               Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true";
    }

    private void ConfigureInMemoryDatabase(WebApplicationBuilder builder)
    {
        Console.WriteLine("Using in-memory database");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestingDb"));
    }

    private void ConfigureSqliteDatabase(WebApplicationBuilder builder)
    {
        var sqliteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(sqliteConnectionString))
        {
            Console.WriteLine("Creating SQLite connection string with default path");
            sqliteConnectionString = "Data Source=app.db";
            builder.Configuration["ConnectionStrings:DefaultConnection"] = sqliteConnectionString;
        }

        Console.WriteLine("Using SQLite for development: " + sqliteConnectionString);
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(sqliteConnectionString,
                sqliteOptions => sqliteOptions.MigrationsAssembly("SG01G02_MVC.Infrastructure")));
    }

    private void ConfigurePostgreSqlDatabase(WebApplicationBuilder builder)
    {
        var connectionString = GetPostgreSqlConnectionString(builder);
        Console.WriteLine("Using PostgreSQL connection string from configuration");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("SG01G02_MVC.Infrastructure")));
    }

    private string GetPostgreSqlConnectionString(WebApplicationBuilder builder)
    {
        var directEnvConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(directEnvConnectionString))
        {
            Console.WriteLine("Using PostgreSQL connection string directly from environment variable");
            return directEnvConnectionString;
        }

        var configConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");
        if (!string.IsNullOrEmpty(configConnectionString))
        {
            return configConnectionString;
        }

        throw new InvalidOperationException(
            "No database connection string available. Set POSTGRES_CONNECTION_STRING as an environment variable " +
            "or configure DefaultConnection for development.");
    }

    private bool IsInMemoryDatabase(AppDbContext db) => db.Database.ProviderName?.Contains("InMemory") ?? false;
    private bool IsSqliteDatabase(AppDbContext db) => db.Database.ProviderName?.Contains("Sqlite") ?? false;
    private bool IsPostgreSqlDatabase(AppDbContext db) => db.Database.ProviderName?.Contains("Npgsql") ?? false;

    private void HandlePostgreSqlMigrations(AppDbContext db, ILogger logger)
    {
        logger.LogInformation("Applying migrations to PostgreSQL...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");

        // Handle your existing PostgreSQL migration logic here...
        // (I'll skip the detailed implementation to keep this example focused)
    }
}