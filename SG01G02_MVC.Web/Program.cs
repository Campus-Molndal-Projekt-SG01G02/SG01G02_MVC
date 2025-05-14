using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.Repositories;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Infrastructure.External;
using SG01G02_MVC.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using SG01G02_MVC.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Azure Key Vault configuration
ConfigureKeyVault(builder);

// Database configuration
ConfigureDatabase(builder);

// Register services
RegisterServices(builder);

var app = builder.Build();

// Configure HTTP pipeline
ConfigureApp(app);

// Run the application
app.Run();


// TODO - Refactor these

// ----- Helper methods -----

void ConfigureKeyVault(WebApplicationBuilder builder)
{
    // Get Key Vault URL from configuration or environment variables
    var keyVaultUrl = builder.Configuration["KeyVault:Uri"]
        ?? Environment.GetEnvironmentVariable("KEY_VAULT_URL");

    // If not direct URL, build from name if available
    if (string.IsNullOrEmpty(keyVaultUrl))
    {
        var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
        if (!string.IsNullOrEmpty(keyVaultName))
        {
            keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
        }
    }

    if (string.IsNullOrEmpty(keyVaultUrl))
    {
        // Only log warning in development environment, throw exception in production
        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Key Vault URL is not configured.");
        }

        Console.WriteLine("Warning: No Key Vault URL available, using local settings.");
        return;
    }

    try
    {
        var keyVaultService = new KeyVaultService(keyVaultUrl);
        builder.Services.AddSingleton<IKeyVaultService>(keyVaultService);
        Console.WriteLine($"Connected to Azure Key Vault: {keyVaultUrl}");

        // Retrieve and configure Blob Storage connection string
        var blobConnectionString = keyVaultService.GetSecret("BlobConnectionString");
        if (!string.IsNullOrEmpty(blobConnectionString))
        {
            // IMPORTANT: Store in BOTH locations for backward compatibility
            builder.Configuration["BlobStorageSettings:ConnectionString"] = blobConnectionString;
            builder.Configuration["BlobConnectionString"] = blobConnectionString;  // Add this line
            Console.WriteLine("BlobConnectionString retrieved from Key Vault");
        }

        // Retrieve database connection string (used in ConfigureDatabase)
        var dbConnectionString = keyVaultService.GetSecret("PostgresConnectionString");
        if (!string.IsNullOrEmpty(dbConnectionString))
        {
            builder.Configuration["ConnectionStrings:PostgreSQL"] = dbConnectionString;
            Console.WriteLine("PostgresConnectionString retrieved from Key Vault");
        }
    }
    catch (Exception ex)
    {
        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException($"Could not connect to Key Vault: {ex.Message}", ex);
        }

        Console.WriteLine($"Warning: Could not connect to Key Vault: {ex.Message}");
    }
}

void ConfigureDatabase(WebApplicationBuilder builder)
{
    // Check if we're in testing mode or explicitly using in-memory database
    if (builder.Environment.IsEnvironment("Testing") ||
        Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
    {
        Console.WriteLine("Using in-memory database for testing");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestingDb"));
        return;
    }

    // For development, use SQLite unless PostgreSQL is explicitly requested
    if (builder.Environment.IsDevelopment() &&
        Environment.GetEnvironmentVariable("USE_POSTGRES_IN_DEV") != "true")
    {
        var sqliteConnectionString = builder.Configuration["ConnectionStrings:DefaultConnection"] ??
                                   Environment.GetEnvironmentVariable("DefaultConnection");

        if (string.IsNullOrEmpty(sqliteConnectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing for development.");
        }

        Console.WriteLine("Using development SQLite");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));
        return;
    }

    // For production or when PostgreSQL is explicitly requested in development
    var postgresConnectionString = builder.Configuration["ConnectionStrings:PostgreSQL"] ??
                                 Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

    if (!string.IsNullOrEmpty(postgresConnectionString))
    {
        Console.WriteLine("Using PostgreSQL");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));
        return;
    }

    // If we reached this point, we couldn't find any valid connection string
    throw new InvalidOperationException(
        "No database connection string available. Please configure DefaultConnection for development " +
        "or POSTGRES_CONNECTION_STRING/Key Vault for production.");
}

void ApplyDatabaseMigrations(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Database provider: {Provider}", db.Database.ProviderName);

        // Handle different database types
        if (db.Database.ProviderName?.Contains("Sqlite") ?? false)
        {
            // For SQLite, just ensure the database exists
            logger.LogInformation("SQLite database, ensuring it's created...");
            db.Database.EnsureCreated();
        }
        else if (db.Database.ProviderName?.Contains("Npgsql") ?? false)
        {
            // For PostgreSQL, apply migrations
            logger.LogInformation("PostgreSQL database, running migrations...");

            if (db.Database.CanConnect())
            {
                // Ensure the migrations history table exists
                db.Database.EnsureCreated();
                // Apply any pending migrations
                db.Database.Migrate();
                logger.LogInformation("Database migration completed successfully");
            }
            else
            {
                logger.LogError("Cannot connect to PostgreSQL database - skipping migration");
            }
        }
        else if (db.Database.ProviderName?.Contains("InMemory") ?? false)
        {
            logger.LogInformation("Using InMemory database - no migrations needed");
        }
        else
        {
            logger.LogWarning("Unknown database type: {ProviderName}", db.Database.ProviderName);
        }
    }
    catch (Exception ex)
    {
        // Log the exception but allow the application to continue
        // If database access is critical, it will fail on first database access
        Console.WriteLine($"CRITICAL ERROR during migration: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}

void RegisterServices(WebApplicationBuilder builder)
{
    // Basic services
    builder.Services.AddControllersWithViews();

    // Check if BlobConnectionString exists in either location
    var blobConnectionString = builder.Configuration["BlobStorageSettings:ConnectionString"];

    // If it's in the BlobStorageSettings section, copy it to root level for backward compatibility
    if (!string.IsNullOrEmpty(blobConnectionString))
    {
        builder.Configuration["BlobConnectionString"] = blobConnectionString;
        Console.WriteLine("Copied BlobConnectionString from BlobStorageSettings section to root level");
    }
    // If it's only in root level, copy it to the BlobStorageSettings section
    else if (!string.IsNullOrEmpty(builder.Configuration["BlobConnectionString"]))
    {
        builder.Configuration["BlobStorageSettings:ConnectionString"] = builder.Configuration["BlobConnectionString"];
        Console.WriteLine("Copied BlobConnectionString from root level to BlobStorageSettings section");
    }

    // Configure BlobStorage container name if missing
    if (string.IsNullOrEmpty(builder.Configuration["BlobStorageSettings:ContainerName"]))
    {
        builder.Configuration["BlobStorageSettings:ContainerName"] = "product-images";
    }

    // Repository and service registrations
    builder.Services.AddScoped<IProductRepository, EfProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserSessionService, UserSessionService>();
    
    // Register BlobStorageService
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
    
    // Add Review services (from main branch)
    builder.Services.AddHttpClient<IReviewApiClient, ReviewApiClient>();
    builder.Services.AddScoped<IReviewService, ReviewService>();

    // Session and authentication
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.IdleTimeout = TimeSpan.FromMinutes(30);
    });

    builder.Services.AddAuthentication("CookieAuth")
        .AddCookie("CookieAuth", config =>
        {
            config.LoginPath = "/Login/Index";
        });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("Database");
}

void ConfigureApp(WebApplication app)
{
    // Run database migration
    ApplyDatabaseMigrations(app);

    // Configure HTTP pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }

    app.UseRouting();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();
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
}