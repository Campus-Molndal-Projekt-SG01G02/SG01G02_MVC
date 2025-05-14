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

// Configure services
ConfigureServices(builder);

var app = builder.Build();

// Configure the HTTP request pipeline
ConfigureApp(app);

app.Run();

// ===== Helper Methods =====

void ConfigureServices(WebApplicationBuilder builder)
{
    // 1. Configure Key Vault (if needed)
    ConfigureKeyVault(builder);

    // 2. Configure database
    ConfigureDatabase(builder);

    // 3. Register common services
    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor();

    // 4. Register application services
    builder.Services.AddScoped<IProductRepository, EfProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserSessionService, UserSessionService>();

    // 5. Configure and register BlobStorage
    ConfigureBlobStorage(builder);

    // 6. Register other services
    builder.Services.AddHttpClient<IReviewApiClient, ReviewApiClient>();
    builder.Services.AddScoped<IReviewService, ReviewService>();

    // 7. Session and authentication
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

    // 8. Health checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("Database");
}

void ConfigureKeyVault(WebApplicationBuilder builder)
{
    // Skip Key Vault in testing environment
    if (builder.Environment.IsEnvironment("Testing"))
    {
        Console.WriteLine("Testing environment - using default configuration values");
        builder.Configuration["BlobStorageSettings:ConnectionString"] = "UseDevelopmentStorage=true";
        builder.Configuration["BlobConnectionString"] = "UseDevelopmentStorage=true";
        return;
    }

    string keyVaultUrl = builder.Configuration["KeyVault:Uri"]
        ?? Environment.GetEnvironmentVariable("KEY_VAULT_URL")
        ?? GetKeyVaultUrlFromName();

    if (string.IsNullOrEmpty(keyVaultUrl))
    {
        if (!builder.Environment.IsDevelopment())
            throw new InvalidOperationException("Key Vault URL is not configured");

        Console.WriteLine("No Key Vault URL available - using local settings");
        return;
    }

    try
    {
        var keyVaultService = new KeyVaultService(keyVaultUrl);
        builder.Services.AddSingleton<IKeyVaultService>(keyVaultService);
        Console.WriteLine($"Connected to Azure Key Vault: {keyVaultUrl}");

        // Store retrieved secrets in configuration
        StoreSecret(keyVaultService, "BlobConnectionString",
            new[] { "BlobStorageSettings:ConnectionString", "BlobConnectionString" });

        StoreSecret(keyVaultService, "PostgresConnectionString",
            new[] { "ConnectionStrings:PostgreSQL" });
    }
    catch (Exception ex)
    {
        HandleKeyVaultError(ex, builder.Environment.IsDevelopment());
    }

    // Helper methods
    string GetKeyVaultUrlFromName()
    {
        var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
        return !string.IsNullOrEmpty(keyVaultName)
            ? $"https://{keyVaultName}.vault.azure.net/"
            : null;
    }

    void StoreSecret(KeyVaultService service, string secretName, string[] configKeys)
    {
        try
        {
            var value = service.GetSecret(secretName);
            if (!string.IsNullOrEmpty(value))
            {
                foreach (var key in configKeys)
                    builder.Configuration[key] = value;

                Console.WriteLine($"{secretName} retrieved from Key Vault");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving {secretName}: {ex.Message}");
        }
    }

    void HandleKeyVaultError(Exception ex, bool isDevelopment)
    {
        if (!isDevelopment)
            throw new InvalidOperationException($"Could not connect to Key Vault: {ex.Message}", ex);

        Console.WriteLine($"Warning: Could not connect to Key Vault: {ex.Message}");
    }
}

void ConfigureDatabase(WebApplicationBuilder builder)
{
    // First try what you specified in settings
    if (builder.Environment.IsEnvironment("Testing") ||
        Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
    {
        Console.WriteLine("Using in-memory database");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestingDb"));
        return;
    }

    // Check for PostgreSQL configuration first
    bool usePostgresInDev = Environment.GetEnvironmentVariable("USE_POSTGRES_IN_DEV") == "true" ||
                          builder.Configuration.GetValue<bool>("UsePostgresInDev");

    if (usePostgresInDev || !builder.Environment.IsDevelopment())
    {
        var postgresConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");

        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            // For development, test connection before committing to Postgres
            if (builder.Environment.IsDevelopment())
            {
                try
                {
                    Console.WriteLine("Testing PostgreSQL connection...");
                    using var conn = new Npgsql.NpgsqlConnection(postgresConnectionString);
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try
                    {
                        // Test if we can establish a connection within 5 seconds
                        conn.OpenAsync(cancellationTokenSource.Token).GetAwaiter().GetResult();
                        conn.Close();
                        Console.WriteLine("PostgreSQL connection successful - using PostgreSQL");

                        builder.Services.AddDbContext<AppDbContext>(options =>
                            options.UseNpgsql(postgresConnectionString));
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("PostgreSQL connection timed out - falling back to SQLite");
                        // Fall through to SQLite configuration
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PostgreSQL connection failed: {ex.Message} - falling back to SQLite");
                    // Fall through to SQLite configuration
                }
            }
            else
            {
                // In production, always try to use PostgreSQL
                Console.WriteLine("Using PostgreSQL database");
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(postgresConnectionString));
                return;
            }
        }
    }

    // Fall back to SQLite for development
    if (builder.Environment.IsDevelopment())
    {
        var sqliteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(sqliteConnectionString))
        {
            Console.WriteLine("No SQLite connection string found - using in-memory database");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("DevelopmentDb"));
        }
        else
        {
            Console.WriteLine("Using SQLite database for development");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(sqliteConnectionString));
        }
        return;
    }

    // If we got here, we have no database configuration
    throw new InvalidOperationException(
        "No database connection string available. Please configure DefaultConnection for development " +
        "or PostgreSQL connection string for production.");
}

void ConfigureBlobStorage(WebApplicationBuilder builder)
{
    // Ensure BlobConnectionString is available in both locations
    var blobConnectionString = builder.Configuration["BlobStorageSettings:ConnectionString"];

    if (!string.IsNullOrEmpty(blobConnectionString))
    {
        builder.Configuration["BlobConnectionString"] = blobConnectionString;
    }
    else if (!string.IsNullOrEmpty(builder.Configuration["BlobConnectionString"]))
    {
        builder.Configuration["BlobStorageSettings:ConnectionString"] = builder.Configuration["BlobConnectionString"];
    }

    // Set default container name if missing
    if (string.IsNullOrEmpty(builder.Configuration["BlobStorageSettings:ContainerName"]))
    {
        builder.Configuration["BlobStorageSettings:ContainerName"] = "product-images";
    }

    // Register BlobStorageService
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
}

void ConfigureApp(WebApplication app)
{
    // Configure the HTTP pipeline
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
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.Select(e => new
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                })
            }));
        }
    });

    // Apply database migrations (with simpler implementation)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Database provider: {Provider}", db.Database.ProviderName);

            if (db.Database.ProviderName?.Contains("InMemory") ?? false)
            {
                logger.LogInformation("Using in-memory database - no migrations needed");
            }
            else if (db.Database.ProviderName?.Contains("Sqlite") ?? false)
            {
                logger.LogInformation("Ensuring SQLite database is created");
                db.Database.EnsureCreated();
            }
            else if (db.Database.ProviderName?.Contains("Npgsql") ?? false)
            {
                // Fix for PostgreSQL connectivity issues - test connection without trying to close
                logger.LogInformation("Testing PostgreSQL connection...");

                try
                {
                    // Just test if we can connect - don't try to manually close
                    var canConnect = db.Database.CanConnect();

                    if (canConnect)
                    {
                        logger.LogInformation("Successfully connected to PostgreSQL, applying migrations");
                        db.Database.EnsureCreated();
                        db.Database.Migrate();
                    }
                    else
                    {
                        logger.LogError("Cannot connect to PostgreSQL - check firewall rules and credentials");
                    }
                }
                catch (Exception dbEx)
                {
                    logger.LogError(dbEx, "Error connecting to PostgreSQL");
                    logger.LogError("Check firewall rules, credentials, and network connectivity");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database initialization");
        }
    }
}