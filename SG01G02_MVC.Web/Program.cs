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
using System.Collections;

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
    // Skip Key Vault in test environment
    if (builder.Environment.IsEnvironment("Testing") ||
        Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
    {
        Console.WriteLine("Test environment - using default values for configuration");
        builder.Configuration["BlobStorageSettings:ConnectionString"] = "UseDevelopmentStorage=true";
        builder.Configuration["BlobConnectionString"] = "UseDevelopmentStorage=true";
        return;
    }

    // Print all environment variables for debugging
    Console.WriteLine("=== Environment variables for debugging ===");
    foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        string key = env.Key.ToString();
        if (key.Contains("KEY_VAULT") || key.Contains("POSTGRES") || key.Contains("AZURE"))
        {
            string value = key.Contains("CONNECTION_STRING") || key.Contains("TOKEN") || key.Contains("KEY")
                ? "***" : env.Value.ToString();
            Console.WriteLine($"{key}={value}");
        }
    }
    Console.WriteLine("=====================================");

    // Check if POSTGRES_CONNECTION_STRING exists directly as an environment variable
    var postgresConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(postgresConnectionString))
    {
        Console.WriteLine("Found POSTGRES_CONNECTION_STRING in environment variables, using it directly");
        builder.Configuration["ConnectionStrings:PostgreSQL"] = postgresConnectionString;
    }

    // Check Key Vault configuration
    string keyVaultUrl = builder.Configuration["KeyVault:Uri"]
                      ?? Environment.GetEnvironmentVariable("KEY_VAULT_URL");

    string keyVaultName = builder.Configuration["KeyVault:Name"]
                       ?? Environment.GetEnvironmentVariable("KEY_VAULT_NAME");

    // Print values for debugging
    Console.WriteLine($"Key Vault URL from configuration: '{keyVaultUrl}'");
    Console.WriteLine($"Key Vault Name from configuration: '{keyVaultName}'");

    // Create URL from name if needed
    if (string.IsNullOrEmpty(keyVaultUrl) && !string.IsNullOrEmpty(keyVaultName) &&
        !keyVaultName.Contains("your-key-vault-name") && !keyVaultName.Contains("${"))
    {
        keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
        Console.WriteLine($"Generated Key Vault URL from name: '{keyVaultUrl}'");
    }

    // If we still don't have a valid URL after our attempts
    if (string.IsNullOrEmpty(keyVaultUrl) ||
        keyVaultUrl.Contains("your-key-vault-name") ||
        keyVaultUrl.Contains("${") ||
        keyVaultUrl.Contains("undefined"))
    {
        Console.WriteLine("WARNING: No valid Key Vault URL found. Using environment variables directly if available.");

        // Here we need to ensure that the database connection is configured if available
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            Console.WriteLine("Using POSTGRES_CONNECTION_STRING from environment variables as fallback");
            builder.Configuration["ConnectionStrings:PostgreSQL"] = postgresConnectionString;
        }
        else
        {
            Console.WriteLine("WARNING: No PostgreSQL connection string available in environment variables.");
        }

        return;
    }

    try
    {
        var keyVaultService = new KeyVaultService(keyVaultUrl);

        if (!keyVaultService.IsAvailable)
        {
            Console.WriteLine("Key Vault service is not available - using fallback configuration");

            // Ensure we use POSTGRES_CONNECTION_STRING directly if available
            if (!string.IsNullOrEmpty(postgresConnectionString))
            {
                Console.WriteLine("Using POSTGRES_CONNECTION_STRING from environment variables as fallback");
                builder.Configuration["ConnectionStrings:PostgreSQL"] = postgresConnectionString;
            }

            return;
        }

        builder.Services.AddSingleton<IKeyVaultService>(keyVaultService);
        Console.WriteLine($"Connected to Azure Key Vault: {keyVaultUrl}");

        // Retrieve secrets only if they are not already in the configuration
        if (string.IsNullOrEmpty(builder.Configuration["ConnectionStrings:PostgreSQL"]))
        {
            TryStoreSecret(keyVaultService, "PostgresConnectionString",
                new[] { "ConnectionStrings:PostgreSQL" });
        }

        TryStoreSecret(keyVaultService, "BlobConnectionString",
            new[] { "BlobStorageSettings:ConnectionString", "BlobConnectionString" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Error connecting to Key Vault: {ex.Message}");
        Console.WriteLine("Using environment variables directly if available");

        // Ensure we use POSTGRES_CONNECTION_STRING directly if available
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            Console.WriteLine("Using POSTGRES_CONNECTION_STRING from environment variables as fallback");
            builder.Configuration["ConnectionStrings:PostgreSQL"] = postgresConnectionString;
        }
    }

    // Verify that we have a database connection string
    if (string.IsNullOrEmpty(builder.Configuration["ConnectionStrings:PostgreSQL"]))
    {
        Console.WriteLine("CRITICAL: No PostgreSQL connection string available after all attempts!");
    }
    else
    {
        Console.WriteLine("PostgreSQL connection string is configured (value not shown for security reasons)");
    }

    void TryStoreSecret(IKeyVaultService service, string secretName, string[] configKeys)
    {
        try
        {
            var value = service.GetSecret(secretName);
            if (!string.IsNullOrEmpty(value))
            {
                foreach (var key in configKeys)
                    builder.Configuration[key] = value;

                Console.WriteLine($"Retrieved '{secretName}' from Key Vault");
            }
            else
            {
                Console.WriteLine($"WARNING: '{secretName}' was empty or null in Key Vault");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving '{secretName}': {ex.Message}");
        }
    }
}

void ConfigureDatabase(WebApplicationBuilder builder)
{
    // If we are using in-memory database
    if (builder.Environment.IsEnvironment("Testing") ||
        Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
    {
        Console.WriteLine("Using in-memory database");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestingDb"));
        return;
    }

    // DIRECT FALLBACK: Check if we have a direct environment variable for PostgreSQL first
    var directEnvConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(directEnvConnectionString))
    {
        Console.WriteLine("Using PostgreSQL connection string directly from environment variable");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(directEnvConnectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("SG01G02_MVC.Infrastructure")));
        return;
    }

    // Also check the configuration (which may have been filled by the Key Vault service)
    var configConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    if (!string.IsNullOrEmpty(configConnectionString))
    {
        Console.WriteLine("Using PostgreSQL connection string from configuration");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configConnectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("SG01G02_MVC.Infrastructure")));
        return;
    }

    // Fallback for development
    if (builder.Environment.IsDevelopment())
    {
        // For development, use SQLite
        var sqliteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(sqliteConnectionString))
        {
            Console.WriteLine("Using SQLite for development");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(sqliteConnectionString,
                    sqliteOptions => sqliteOptions.MigrationsAssembly("SG01G02_MVC.Infrastructure")));
            return;
        }

        // If no SQLite connection exists, use in-memory also for development
        Console.WriteLine("No SQLite connection string found - using in-memory database for development");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("DevelopmentDb"));
        return;
    }

    // If we got here, we have no valid database option
    throw new InvalidOperationException(
        "No database connection string available. Set POSTGRES_CONNECTION_STRING as an environment variable " +
        "or configure DefaultConnection for development.");
}

void ConfigureBlobStorage(WebApplicationBuilder builder)
{
    // Debug: Print available blob configuration
    Console.WriteLine("=== Blob Storage Configuration ===");
    Console.WriteLine($"BlobStorageSettings:ConnectionString: {(string.IsNullOrEmpty(builder.Configuration["BlobStorageSettings:ConnectionString"]) ? "missing" : "exists")}");
    Console.WriteLine($"BlobConnectionString: {(string.IsNullOrEmpty(builder.Configuration["BlobConnectionString"]) ? "missing" : "exists")}");

    // Get connection string from configuration
    var blobConnectionString = builder.Configuration["BlobStorageSettings:ConnectionString"];

    // Check alternative configuration key if primary is missing
    if (string.IsNullOrEmpty(blobConnectionString))
    {
        blobConnectionString = builder.Configuration["BlobConnectionString"];
    }

    // Check environment variables if still missing
    if (string.IsNullOrEmpty(blobConnectionString))
    {
        blobConnectionString = Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(blobConnectionString))
        {
            Console.WriteLine("Found BLOB_CONNECTION_STRING in environment variables");
            builder.Configuration["BlobStorageSettings:ConnectionString"] = blobConnectionString;
            builder.Configuration["BlobConnectionString"] = blobConnectionString;
        }
    }

    // Use development/fallback storage if still missing
    if (string.IsNullOrEmpty(blobConnectionString))
    {
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine("No Blob connection string found - using local development storage");
            blobConnectionString = "UseDevelopmentStorage=true";
        }
        else
        {
            Console.WriteLine("WARNING: No Blob connection string found in production - using fallback");
            // Use a special value that BlobStorageService will recognize as "test mode"
            blobConnectionString = "InMemoryEmulation=true";
        }

        // Store in both configuration locations
        builder.Configuration["BlobStorageSettings:ConnectionString"] = blobConnectionString;
        builder.Configuration["BlobConnectionString"] = blobConnectionString;
    }

    // Set default container name if missing
    if (string.IsNullOrEmpty(builder.Configuration["BlobStorageSettings:ContainerName"]))
    {
        builder.Configuration["BlobStorageSettings:ContainerName"] = "product-images";
    }

    Console.WriteLine($"Using Blob connection string: {(blobConnectionString.Contains("AccountKey") ? "Real Azure Blob" : blobConnectionString)}");
    Console.WriteLine($"Container name: {builder.Configuration["BlobStorageSettings:ContainerName"]}");

    // Register BlobStorageService
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
}

// I ConfigureApp-metoden
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
    app.UseStaticFiles();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

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

    // Initialize database - Apply migrations or ensure schema is created
    InitializeDatabase(app);
}

void InitializeDatabase(WebApplication app)
{
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
                logger.LogInformation("Testing PostgreSQL connection...");

                try
                {
                    var canConnect = db.Database.CanConnect();

                    if (canConnect)
                    {
                        logger.LogInformation("Successfully connected to PostgreSQL");

                        try
                        {
                            // Ensure migrations history table exists
                            db.Database.ExecuteSqlRaw(
                                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (" +
                                "\"MigrationId\" character varying(150) NOT NULL, " +
                                "\"ProductVersion\" character varying(32) NOT NULL, " +
                                "CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"));"
                            );
                            logger.LogInformation("Ensured migrations history table exists");

                            // Check for pending migrations
                            var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                            logger.LogInformation("Found {Count} pending migrations", pendingMigrations.Count);

                            if (pendingMigrations.Any())
                            {
                                logger.LogInformation("Applying migrations: {Migrations}",
                                    string.Join(", ", pendingMigrations));

                                db.Database.Migrate();
                                logger.LogInformation("Successfully applied migrations");
                            }
                            else
                            {
                                // If no migrations are pending but tables are missing, use EnsureCreated
                                try
                                {
                                    // Check if Users table exists as an indicator
                                    var userTableExists = false;
                                    var command = db.Database.GetDbConnection().CreateCommand();
                                    command.CommandText = @"
                                        SELECT EXISTS (
                                            SELECT FROM information_schema.tables
                                            WHERE table_schema = 'public' AND table_name = 'Users'
                                        );";

                                    db.Database.OpenConnection();
                                    var result = command.ExecuteScalar();
                                    userTableExists = result != null && (result.ToString() == "1" || result.ToString().ToLower() == "true");
                                    db.Database.CloseConnection();

                                    if (!userTableExists)
                                    {
                                        logger.LogInformation("No migrations found and tables are missing. Creating database schema...");
                                        db.Database.EnsureCreated();
                                        logger.LogInformation("Database schema created successfully");
                                    }
                                    else
                                    {
                                        logger.LogInformation("Database schema already exists, no migrations to apply");
                                    }
                                }
                                catch (Exception tableEx)
                                {
                                    logger.LogError(tableEx, "Error checking database tables");

                                    // Fallback: Try EnsureCreated anyway
                                    logger.LogWarning("Attempting to ensure database is created as fallback");
                                    db.Database.EnsureCreated();
                                }
                            }
                        }
                        catch (InvalidOperationException migEx) when (migEx.Message.Contains("pending model changes"))
                        {
                            logger.LogWarning("Model has pending changes not reflected in migrations");
                            logger.LogWarning("Using EnsureCreated as fallback, which may cause data loss!");

                            // This might cause data loss if tables already exist, but better than a crash
                            db.Database.EnsureCreated();
                            logger.LogInformation("Database schema updated with EnsureCreated");
                        }
                        catch (Exception migEx)
                        {
                            logger.LogError(migEx, "Error applying migrations");

                            // Try EnsureCreated as a last resort
                            logger.LogWarning("Attempting to ensure database is created as fallback");
                            db.Database.EnsureCreated();
                        }
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