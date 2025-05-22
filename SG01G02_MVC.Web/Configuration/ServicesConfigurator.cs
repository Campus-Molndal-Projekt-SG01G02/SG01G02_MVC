using Microsoft.AspNetCore.Http.Features;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.Configuration;
using SG01G02_MVC.Infrastructure.External;
using SG01G02_MVC.Infrastructure.Repositories;
using SG01G02_MVC.Infrastructure.Services;
using SG01G02_MVC.Web.Services;

namespace SG01G02_MVC.Web.Configuration;

public class ServicesConfigurator
{
    public void Configure(WebApplicationBuilder builder, KeyVaultConfigurator keyVaultConfig,
        DatabaseConfigurator databaseConfig, BlobStorageConfigurator blobStorageConfig)
    {
        // Configure Key Vault (if needed)
        keyVaultConfig.Configure(builder);

        // Configure database
        databaseConfig.Configure(builder);

        // Register common services
        RegisterCommonServices(builder);
        RegisterApplicationServices(builder);

        // Configure blob storage
        blobStorageConfig.Configure(builder);

        RegisterHttpClients(builder);
        RegisterSessionAndAuthentication(builder);
        RegisterHealthChecksAndSwagger(builder);
    }

    private void RegisterCommonServices(WebApplicationBuilder builder)
    {
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
    }

    private void RegisterApplicationServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IProductRepository, EfProductRepository>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserSessionService, UserSessionService>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
    }

    private void RegisterHttpClients(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var services = builder.Services;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // --- External API client registration ---
        var reviewApiUrl = config["ReviewApiURL"];
        var reviewApiKey = config["ReviewApiKey"];

        if (!string.IsNullOrWhiteSpace(reviewApiUrl))
        {
            services.AddHttpClient("ExternalReviewApi", client =>
            {
                client.BaseAddress = new Uri(reviewApiUrl);
                if (!string.IsNullOrWhiteSpace(reviewApiKey))
                    client.DefaultRequestHeaders.Add("x-api-key", reviewApiKey);
            });
        }

        // --- Mock API client registration ---
        var mockApiUrl = config["MockReviewApiURL"];
        var mockApiKey = config["MockReviewApiKey"];

        if (!string.IsNullOrWhiteSpace(mockApiUrl))
        {
            services.AddHttpClient("MockReviewApi", client =>
            {
                client.BaseAddress = new Uri(mockApiUrl);
                if (!string.IsNullOrWhiteSpace(mockApiKey))
                    client.DefaultRequestHeaders.Add("x-api-key", mockApiKey);
            });
        }

        // --- Register DualReviewApiClient ---
        services.AddScoped<IReviewApiClient>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<DualReviewApiClient>>();
            var externalLogger = sp.GetRequiredService<ILogger<ReviewApiClient>>();
            var mockLogger = sp.GetRequiredService<ILogger<MockReviewApiClient>>();

            var config = sp.GetRequiredService<IConfiguration>();

            IReviewApiClient? externalClient = null;
            IReviewApiClient? fallbackClient = null;

            if (!string.IsNullOrWhiteSpace(reviewApiUrl))
            {
                externalClient = new ReviewApiClient(httpFactory.CreateClient("ExternalReviewApi"), config, externalLogger);
            }

            if (!string.IsNullOrWhiteSpace(mockApiUrl))
            {
                fallbackClient = new MockReviewApiClient(httpFactory.CreateClient("MockReviewApi"), config, mockLogger);
            }

            if (externalClient != null && fallbackClient != null)
            {
                return new DualReviewApiClient(externalClient, fallbackClient, logger);
            }
            else if (fallbackClient != null)
            {
                logger.LogWarning("External API not configured. Using MockReviewApiClient only.");
                return fallbackClient;
            }
            else
            {
                throw new InvalidOperationException("No valid API configuration found for reviews.");
            }
        });
    }

    private void RegisterSessionAndAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.IdleTimeout = TimeSpan.FromMinutes(30);
        });

        builder.Services.AddAuthentication("CookieAuth")
            .AddCookie("CookieAuth", config => { config.LoginPath = "/Login/Index"; });
    }

    private void RegisterHealthChecksAndSwagger(WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("Database");

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }
}