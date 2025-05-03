using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.Repositories;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Setup Azure Key Vault in non-development environments
bool keyVaultAvailable = false;
if (!builder.Environment.IsDevelopment())
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

                Console.WriteLine($"Successfully connected to Azure Key Vault: {keyVaultUrl}");
                keyVaultAvailable = true;
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
        // Use only the environment variable for the connection string
        var postgresConnString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

        if (!string.IsNullOrEmpty(postgresConnString))
        {
            // Mask the connection string for logging (remove the password)
            var sanitizedConnString = System.Text.RegularExpressions.Regex.Replace(
                postgresConnString,
                "Password=([^;]*)",
                "Password=***");

            Console.WriteLine($"Using PostgreSQL connection: {sanitizedConnString}");
            options.UseNpgsql(postgresConnString);
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Only allow SQLite fallback in development
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine("Using SQLite connection (development only)");
            options.UseSqlite(connectionString);
        }
        else
        {
            // In production, if no connection string is available, throw an exception
            throw new InvalidOperationException("PostgreSQL connection string is missing in environment variable.");
        }
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
builder.Services.AddHealthChecks().AddCheck("Database", () =>
{
    try
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var canConnect = db.Database.CanConnect();

        Console.WriteLine($"Health check - Database connection: {(canConnect ? "Success" : "Failed")}");

        return canConnect
            ? HealthCheckResult.Healthy("Database connection is working.")
            : HealthCheckResult.Unhealthy("Cannot connect to database.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Health check - Database error: {ex.Message}");
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
        })
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(result));
}
});

// Run the application
app.Run();