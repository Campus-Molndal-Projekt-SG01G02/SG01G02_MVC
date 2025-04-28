using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.Repositories;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Setup Azure Key Vault in non-development environments
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");
    var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");

    // If KEY_VAULT_URL is not set, build it from KEY_VAULT_NAME
    if (string.IsNullOrEmpty(keyVaultUrl) && !string.IsNullOrEmpty(keyVaultName))
    {
        keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
    }

    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        try
        {
            // Use DefaultAzureCredential for MSI/Service Principal authentication
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());

            Console.WriteLine($"Successfully connected to Azure Key Vault: {keyVaultUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to Key Vault: {ex.Message}");
        }
    }
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Configure Entity Framework Core with SQLite or PostgreSQL
// Configure database context (use PostgreSQL in production and
// development with SQLite as fallback in development)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Local development - try PostgreSQL first, fallback to SQLite
        var postgresConnString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

        // Try to build PostgreSQL connection string from individual settings if not found
        if (string.IsNullOrEmpty(postgresConnString))
        {
            var dbUser = builder.Configuration["PostgresUser"];
            var dbPassword = builder.Configuration["PostgresPassword"];
            var dbHost = builder.Configuration["PostgresHost"] ?? "localhost";
            var dbPort = builder.Configuration["PostgresPort"] ?? "5432";
            var dbName = builder.Configuration["PostgresDatabase"] ?? "appdb";

            if (!string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
            {
                postgresConnString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
                Console.WriteLine("Built PostgreSQL connection string from configuration settings");
            }
        }

        if (!string.IsNullOrEmpty(postgresConnString))
        {
            Console.WriteLine("Using PostgreSQL connection in development");
            options.UseNpgsql(postgresConnString);
        }
        else
        {
            var sqliteConnString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
            Console.WriteLine("Using SQLite connection as fallback in development");
            options.UseSqlite(sqliteConnString);
        }
    }
    else
    {
        // Production - PostgreSQL only with Key Vault integration
        var postgresConnString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

        // Try to build connection string from Key Vault secrets if not found
        if (string.IsNullOrEmpty(postgresConnString))
        {
            var dbUser = builder.Configuration["PostgresUser"];
            var dbPassword = builder.Configuration["PostgresPassword"];
            var dbHost = builder.Configuration["PostgresHost"] ?? "10.0.4.4"; // Default to VM IP
            var dbPort = builder.Configuration["PostgresPort"] ?? "5432";
            var dbName = builder.Configuration["PostgresDatabase"] ?? "appdb";

            if (!string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
            {
                postgresConnString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
                Console.WriteLine("Built PostgreSQL connection string from Key Vault secrets");
            }
            else
            {
                throw new InvalidOperationException("PostgreSQL connection information is missing. Ensure Key Vault secrets are properly configured.");
            }
        }

        Console.WriteLine("Using PostgreSQL connection in production");
        options.UseNpgsql(postgresConnString);
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
});

// Add authentication
builder.Services.AddAuthentication("CookieAuth")
.AddCookie("CookieAuth", config =>
{
    config.LoginPath = "/Login/Index"; // fallback if unauthenticated
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("Database", () =>
    {
        try
        {
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return db.Database.CanConnect()
                ? HealthCheckResult.Healthy("Database connection is working.")
                : HealthCheckResult.Unhealthy("Cannot connect to database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Database error: {ex.Message}");
        }
    });

var app = builder.Build();

// Try to connect to the SQLite database, and seed admin user if available.
// If the DB is missing (e.g. during CI/CD), log a warning and render fallback view if needed.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        if (db.Database.CanConnect())
        {
            Console.WriteLine($"Connected to database with provider: {db.Database.ProviderName}");

            if (db.Database.ProviderName?.Contains("Npgsql") == true)
            {
                Console.WriteLine("Applying PostgreSQL database migrations...");
                db.Database.Migrate();
            }

            // Seed default admin user
            SeederHelper.SeedAdminUser(app);
        }
        else
        {
            Console.WriteLine("WARNING: Could not connect to database. No seeding will occur.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database check failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Use routing, authorization, and static assets
app.UseRouting();
app.UseSession(); // Enables session before authorization - very important!
app.UseAuthentication(); // Handles ClaimsPrincipal + CookieAuth
app.UseAuthorization(); // Enables [Authorize] attribute
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.MapHealthChecks("/health", new HealthCheckOptions
{
ResponseWriter = async (context, report) =>
{
    context.Response.ContentType = "application/json";

    var result = new
    {
        Status = report.Status.ToString(),
        Checks = report.Entries.Select(e => new
        {
            Name = e.Key,
            Status = e.Value.Status.ToString(),
            Description = e.Value.Description
        })
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(result));
}
});

// Run the application
app.Run();