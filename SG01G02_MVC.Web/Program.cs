using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.Repositories;
using SG01G02_MVC.Infrastructure.Data;
using SG01G02_MVC.Infrastructure.External;
using SG01G02_MVC.Infrastructure.Services;
using SG01G02_MVC.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using System.Collections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using SG01G02_MVC.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Set the default culture for the application
var cultureInfo = new System.Globalization.CultureInfo("sv-SE");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Register KeyVaultService first
ConfigureKeyVault(builder);

// Then register LoggingService based on KeyVaultService
IKeyVaultService? keyVaultService = null;
try {
    var tempProvider = builder.Services.BuildServiceProvider();
    keyVaultService = tempProvider.GetService<IKeyVaultService>();
} catch (Exception ex) {
    Console.WriteLine($"Warning: Could not resolve KeyVaultService: {ex.Message}");
}

var loggingService = new LoggingService(keyVaultService);
loggingService.ConfigureServices(builder.Services, builder.Configuration);
builder.Services.AddSingleton<ILoggingService>(loggingService);

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

    // Configure file upload size limits
    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.MaxRequestBodySize = 30 * 1024 * 1024; // 30 MB
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30 MB
    });

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 30 * 1024 * 1024; // 30 MB
        options.ValueLengthLimit = 30 * 1024 * 1024; // 30 MB
    });

    // 4. Register application services
    builder.Services.AddScoped<IProductRepository, EfProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserSessionService, UserSessionService>();

    // 5. Configure and register BlobStorage
    ConfigureBlobStorage(builder);

    // 6. Register other services
    // builder.Services.AddHttpClient<IReviewApiClient, ReviewApiClient>();
    // Register external API client
    // builder.Services.AddHttpClient("ExternalReviewApi", client =>
    // {
    //     client.BaseAddress = new Uri(builder.Configuration["ReviewApiURL"]!);
    //     client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["ReviewApiKey"]!);
    // });

    // === REVIEW API CLIENT SETUP ===
    var reviewApiUrl = builder.Configuration["ReviewApiURL"];
    var reviewApiKey = builder.Configuration["ReviewApiKey"];

    if (!string.IsNullOrWhiteSpace(reviewApiUrl))
    {
        builder.Services.AddHttpClient("ExternalReviewApi", client =>
        {
            client.BaseAddress = new Uri(reviewApiUrl);
            // NOTE: Azure Function expects ?code=APIKEY in query, not header
            // So no x-api-key header needed
        });
    }

    builder.Services.AddHttpClient("MockReviewApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["MockReviewApiURL"]!);
        // Again, API key is passed as query param
    });

    // === DUAL WRAPPER ===
    builder.Services.AddScoped<IReviewApiClient>(sp =>
    {
        var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<DualReviewApiClient>>();

        var externalLogger = sp.GetRequiredService<ILogger<ReviewApiClient>>();
        var mockLogger = sp.GetRequiredService<ILogger<MockReviewApiClient>>();

        IReviewApiClient primary;

        if (!string.IsNullOrWhiteSpace(config["ReviewApiURL"]))
        {
            var externalHttp = httpFactory.CreateClient("ExternalReviewApi");
            primary = new ReviewApiClient(externalHttp, config, externalLogger); // ✅ Will build ?code=... internally
        }
        else
        {
            Console.WriteLine("⚠️ ExternalReviewApi not configured. Skipping registration and forcing mock fallback.");
            primary = new DisabledReviewApiClient();
        }

        var fallback = new MockReviewApiClient(httpFactory.CreateClient("MockReviewApi"), config, mockLogger);

        return new DualReviewApiClient(primary, fallback, logger); // ✅ FIXED: was 'external'
    });

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

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureKeyVault(WebApplicationBuilder builder)
{
    // Skip Key Vault completely in Development mode
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("Running locally in Development - skipping Azure Key Vault.");
        return;
    }

    // Skip Key Vault in test environment
    if (builder.Environment.IsEnvironment("Testing") ||
        Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
    {
        Console.WriteLine("Test environment - using default values for configuration");
        return;
    }

    // Print all environment variables for debugging
    Console.WriteLine("=== Environment variables for debugging ===");
    foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        string key = env.Key?.ToString() ?? "";
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

        // Add Review API secrets
        TryStoreSecret(keyVaultService, "ReviewApiURL",
            new[] { "ReviewApiURL" });
        TryStoreSecret(keyVaultService, "ReviewApiKey",
            new[] { "ReviewApiKey" });
        TryStoreSecret(keyVaultService, "MockReviewApiURL",
            new[] { "MockReviewApiURL" });
        TryStoreSecret(keyVaultService, "MockReviewApiKey",
            new[] { "MockReviewApiKey" });

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

    // // TODO: Force fallback to mock for local testing! REMOVE THIS LATER!
    // builder.Configuration["ReviewApiURL"] = "";
    // builder.Configuration["ReviewApiKey"] = "";

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

    // Use SQLite in development
    if (builder.Environment.IsDevelopment())
    {
        // For development, use SQLite
        var sqliteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // Always create the SQLite DB if it doesn't exist
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

        return;
    }

    // Check if we have a valid PostgreSQL connection string
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

    // Get connection string from configuration (should be populated by Key Vault)
    var blobConnectionString = builder.Configuration["BlobStorageSettings:ConnectionString"] ??
                               builder.Configuration["BlobConnectionString"];

    // Check specifically for "UseDevelopmentStorage=true" and replace it with InMemoryEmulation
    if (blobConnectionString == "UseDevelopmentStorage=true")
    {
        Console.WriteLine("WARNING: Connection string is set to UseDevelopmentStorage=true but Azure Storage Emulator is not available");
        Console.WriteLine("Using in-memory fallback for blob storage instead");

        blobConnectionString = "InMemoryEmulation=true";
        builder.Configuration["BlobStorageSettings:ConnectionString"] = blobConnectionString;
        builder.Configuration["BlobConnectionString"] = blobConnectionString;
    }
    else if (string.IsNullOrEmpty(blobConnectionString))
    {
        Console.WriteLine("WARNING: No Blob connection string found in configuration from Key Vault");
        Console.WriteLine("Using fallback test mode for blob storage (no actual blob operations will be performed)");

        // Set to special value that will trigger test mode in BlobStorageService
        blobConnectionString = "InMemoryEmulation=true";
        builder.Configuration["BlobStorageSettings:ConnectionString"] = blobConnectionString;
        builder.Configuration["BlobConnectionString"] = blobConnectionString;
    }
    else
    {
        Console.WriteLine("Found Blob connection string in configuration from Key Vault");
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
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                var ex = error?.Error;
                var message = ex != null ? ex.ToString() : "Unknown error";

                // Log the error
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;
                var sessionId = context.Session.GetString("SessionId") ?? "unknown"; // Lagt till semikolon här

                using (logger.BeginScope(new Dictionary<string, object>
                {
                    ["SessionId"] = sessionId,
                    ["RequestPath"] = context.Request.Path.Value ?? "",
                    ["StatusCode"] = context.Response.StatusCode
                }))
                {
                    logger.LogError(exception, "Ohanterat undantag: {Message}", exception?.Message);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("Ett fel inträffade. Vänligen försök igen senare.");
            });
        });
    }

    app.UseRouting();
    app.UseSession();
    app.UseSessionTracking();
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

    // Middleware to handle larger file uploads on specific endpoints
    app.Use(async (context, next) =>
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var controllerAction = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (controllerAction != null)
            {
                // Check if this path is related to file upload based on name
                var actionName = controllerAction.ActionName.ToLower();
                var controllerName = controllerAction.ControllerName.ToLower();

                bool isUploadRelated =
                    actionName.Contains("upload") ||
                    actionName.Contains("add") ||
                    actionName.Contains("edit") ||
                    actionName.Contains("create") ||
                    controllerName.Contains("admin");

                if (isUploadRelated)
                {
                    // Increase limit for upload-related paths
                    var bodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                    if (bodySizeFeature != null && bodySizeFeature.IsReadOnly == false)
                    {
                        // Set a high limit for these paths
                        bodySizeFeature.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
                        Console.WriteLine($"Increased request size limit for path: {context.Request.Path}");
                    }
                }
            }
        }

        await next();
    });

    // Initialize database - Apply migrations or ensure schema is created
    InitializeDatabase(app);

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
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
            logger.LogInformation("EXACT Database provider name: '{ExactProvider}'", db.Database.ProviderName ?? "null");


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
                logger.LogInformation("Applying migrations to PostgreSQL...");
                db.Database.Migrate();
                logger.LogInformation("Migrations applied successfully.");

                // Check if Products table exists but ImageName column doesn't
                bool productsTableExists = false;
                bool imageNameColumnExists = false;

                try
                {
                    // Create a connection and check both the table and column
                    var conn = db.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                        conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        // Check if Products table exists
                        cmd.CommandText = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Products');";
                        var result = cmd.ExecuteScalar();
                        productsTableExists = result != null && (result.ToString() == "1" || result.ToString().ToLower() == "true");

                        // If table exists, check if ImageName column exists
                        if (productsTableExists)
                        {
                            cmd.CommandText = "SELECT EXISTS (SELECT FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Products' AND column_name = 'ImageName');";
                            result = cmd.ExecuteScalar();
                            imageNameColumnExists = result != null && (result.ToString() == "1" || result.ToString().ToLower() == "true");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error checking database schema");
                }

                // Handle different scenarios
                if (productsTableExists)
                {
                    logger.LogInformation("Products table exists. ImageName column exists: {ImageNameExists}", imageNameColumnExists);

                    if (!imageNameColumnExists)
                    {
                        // Table exists but column doesn't - directly add the column
                        logger.LogInformation("Adding ImageName column directly with SQL");

                        try
                        {
                            // Add the column manually with SQL
                            db.Database.ExecuteSqlRaw(
                                "ALTER TABLE \"Products\" ADD COLUMN IF NOT EXISTS \"ImageName\" text NULL; " +
                                "ALTER TABLE \"Products\" ADD COLUMN IF NOT EXISTS \"ImageDescription\" text NULL; " +
                                "ALTER TABLE \"Products\" ADD COLUMN IF NOT EXISTS \"ExternalReviewApiProductId\" integer NULL;"
                            );

                            // Mark all migrations as applied in the history table
                            db.Database.ExecuteSqlRaw(
                                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (" +
                                "\"MigrationId\" character varying(150) NOT NULL, " +
                                "\"ProductVersion\" character varying(32) NOT NULL, " +
                                "CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"));"
                            );

                            // Add all migrations to the history table to mark them as applied
                            var allMigrations = db.Database.GetMigrations().ToList();
                            foreach (var migration in allMigrations)
                            {
                                db.Database.ExecuteSqlRaw(
                                    $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                                    $"SELECT '{migration}', '9.0.4' " +
                                    $"WHERE NOT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{migration}');"
                                );
                            }

                            logger.LogInformation("Successfully added columns and marked migrations as applied");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error updating schema");
                            throw;
                        }
                    }
                    else
                    {
                        // Both table and column exist - just make sure migrations are marked as applied
                        logger.LogInformation("Schema is already up-to-date. Ensuring migrations are marked as applied");

                        // Create migrations history table if it doesn't exist
                        db.Database.ExecuteSqlRaw(
                            "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (" +
                            "\"MigrationId\" character varying(150) NOT NULL, " +
                            "\"ProductVersion\" character varying(32) NOT NULL, " +
                            "CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"));"
                        );

                        // Add all migrations to the history table
                        var allMigrations = db.Database.GetMigrations().ToList();
                        foreach (var migration in allMigrations)
                        {
                            db.Database.ExecuteSqlRaw(
                                $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                                $"SELECT '{migration}', '9.0.4' " +
                                $"WHERE NOT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{migration}');"
                            );
                        }

                        logger.LogInformation("Successfully marked all migrations as applied");
                    }
                }
                else
                {
                    // Products table doesn't exist - create the schema from scratch
                    logger.LogInformation("Products table doesn't exist. Creating full schema");
                    db.Database.EnsureCreated();
                }
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
}