using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.Repositories;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Azure.Identity;
using SG01G02_MVC.Web.HealthChecks;
using System.Collections;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // Setup Azure Key Vault in non-development environments
    bool keyVaultAvailable = false;

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
            var credential = new DefaultAzureCredential();

            // Explicit test of Azure authentication
            Console.WriteLine("Testing Azure authentication...");

            var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://vault.azure.net/.default" });

            var token = credential.GetToken(tokenRequestContext);

            if (!string.IsNullOrEmpty(token.Token))
            {
                Console.WriteLine("Azure authentication successful!");

                // Proceed with adding KeyVault to the configuration
                builder.Configuration.AddAzureKeyVault(
                    new Uri(keyVaultUrl),
                    credential);

                keyVaultAvailable = true;

                if (keyVaultAvailable)
                {
                    Console.WriteLine("Key Vault configuration is ready for use");
                }
                else
                {
                    Console.WriteLine("Key Vault is not available, using fallback configuration");
                }

                Console.WriteLine($"Successfully connected to Azure Key Vault: {keyVaultUrl}");
            }
            else
            {
                Console.WriteLine("Azure authentication failed: Could not obtain token");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with Azure authentication or Key Vault connection: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }
    else
    {
        Console.WriteLine("No Key Vault URL or name provided in environment variables");
    }
}


/// <summary>
/// This adds functionallity for CI/CD to be able to run smoketest.
/// If environment is test, then it uses an in memory db for testing (smoketest).
/// What it does:
/// - Calls /health to check that the server responds and
///   is healthy and that the database is reachable.
/// - Test the login page (/Login/Index) to ensure it responds.
/// - Test the homepage to ensure it responds.
///
/// Otherwise, it uses the PostgreSQL database connection string from the environment variable.
/// If not found, it falls back to SQLite in development.
/// </summary>

if (builder.Environment.IsEnvironment("Testing") || Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
{
    Console.WriteLine("Using in-memory database for testing");

    // Register in-memory database for testing
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseInMemoryDatabase("TestingDb");
        Console.WriteLine("Configured in-memory database for testing");
    });
}
else
{
    // Configure database context
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        // 1. Check for connection string in environment variables
        var envConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            Console.WriteLine("Using PostgreSQL connection string from environment variable");
            options.UseNpgsql(envConnectionString);
            return;
        }

        // 2. For development, use SQLite
        if (builder.Environment.IsDevelopment())
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            Console.WriteLine("Using development SQLite connection");
            options.UseSqlite(connectionString);
            return;
        }

        // 3. As a last resort in production, try Key Vault
        var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
        if (!string.IsNullOrEmpty(keyVaultName) && keyVaultName != "your-key-vault-name")
        {
            try {
                // Your existing Key Vault code goes here
                Console.WriteLine("Attempting to get connection string from Key Vault");
                var secretClient = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
                var secret = secretClient.GetSecret("PostgresConnectionString");
                var connectionString = secret.Value.Value;

                Console.WriteLine("Using PostgreSQL connection string from Key Vault");
                options.UseNpgsql(connectionString); // Actually use the connection string!
                return;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error accessing Key Vault: {ex.Message}");
            }
        }

        Console.WriteLine("WARNING: No database connection string available in production environment.");

        // If we reach here without returning, throw a clear error
        throw new InvalidOperationException(
            "No database connection string available. Please set POSTGRES_CONNECTION_STRING environment variable.");
    });
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
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

// Health checks used by CI/CD
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("Database");

var app = builder.Build();

// Try to connect to the SQLite database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Database provider: {Provider}", db.Database.ProviderName);

        // Explicit check for Npgsql
        if (db.Database.ProviderName != null)
        {
            bool isNpgsql = db.Database.ProviderName.Contains("Npgsql");
            logger.LogInformation("Is Npgsql provider: {IsNpgsql}", isNpgsql);

            if (isNpgsql)
            {
                logger.LogInformation("Applying PostgreSQL database migrations...");
                db.Database.Migrate();
                logger.LogInformation("Database migrations SUCCESSFULLY APPLIED");

                // Verify that the tables have been created
                var tableCount = db.Model.GetEntityTypes().Count();
                logger.LogInformation("Entity model contains {TableCount} entity types", tableCount);

                // Check if any migrations have been applied
                try
                {
                    var history = db.Database.GetAppliedMigrations().ToList();
                    logger.LogInformation("Applied {Count} migrations: {Migrations}",
                        history.Count, string.Join(", ", history));
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Could not query migration history: {Error}", ex.Message);
                }
            }
            else
            {
                logger.LogWarning("Not using Npgsql provider, no migrations applied");
            }
        }
        else
        {
            logger.LogError("Database provider is NULL");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"CRITICAL ERROR during migrations: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
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
        })
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(result));
}
});

// Run the application
app.Run();