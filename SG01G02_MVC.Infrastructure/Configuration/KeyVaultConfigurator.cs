using System.Collections;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using SG01G02_MVC.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SG01G02_MVC.Infrastructure.Services;

namespace SG01G02_MVC.Infrastructure.Configuration;

public class KeyVaultConfigurator
{
    public void Configure(WebApplicationBuilder builder)
    {
        // Skip Key Vault in test environment
        if (builder.Environment.IsEnvironment("Testing") ||
            Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
        {
            Console.WriteLine("Test environment - using default values for configuration");
            return;
        }

        PrintEnvironmentVariables();

        var postgresConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            Console.WriteLine("Found POSTGRES_CONNECTION_STRING in environment variables, using it directly");
            builder.Configuration["ConnectionStrings:PostgreSQL"] = postgresConnectionString;
        }

        var keyVaultUrl = GetKeyVaultUrl(builder);

        if (string.IsNullOrEmpty(keyVaultUrl) || IsInvalidKeyVaultUrl(keyVaultUrl))
        {
            Console.WriteLine(
                "WARNING: No valid Key Vault URL found. Using environment variables directly if available.");
            HandleFallbackConfiguration(builder, postgresConnectionString);
            return;
        }

        ConfigureKeyVaultService(builder, keyVaultUrl, postgresConnectionString);
    }

    private void PrintEnvironmentVariables()
    {
        Console.WriteLine("=== Environment variables for debugging ===");
        foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
        {
            string key = env.Key?.ToString() ?? "";
            if (key.Contains("KEY_VAULT") || key.Contains("POSTGRES") || key.Contains("AZURE"))
            {
                string? value = key.Contains("CONNECTION_STRING") || key.Contains("TOKEN") || key.Contains("KEY")
                    ? "***"
                    : env.Value?.ToString();

                Console.WriteLine($"{key}={value}");
            }
        }

        Console.WriteLine("=====================================");
    }

    private string? GetKeyVaultUrl(WebApplicationBuilder builder)
    {
        string? keyVaultUrl = builder.Configuration["KeyVault:Uri"] ??
                              Environment.GetEnvironmentVariable("KEY_VAULT_URL");
        string? keyVaultName = builder.Configuration["KeyVault:Name"] ??
                               Environment.GetEnvironmentVariable("KEY_VAULT_NAME");

        Console.WriteLine($"Key Vault URL from configuration: '{keyVaultUrl}'");
        Console.WriteLine($"Key Vault Name from configuration: '{keyVaultName}'");

        // Create URL from name if needed
        if (string.IsNullOrEmpty(keyVaultUrl) && !string.IsNullOrEmpty(keyVaultName) &&
            !keyVaultName.Contains("your-key-vault-name") && !keyVaultName.Contains("${"))
        {
            keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
            Console.WriteLine($"Generated Key Vault URL from name: '{keyVaultUrl}'");
        }

        return keyVaultUrl;
    }

    private bool IsInvalidKeyVaultUrl(string keyVaultUrl)
    {
        return keyVaultUrl.Contains("your-key-vault-name") ||
               keyVaultUrl.Contains("${") ||
               keyVaultUrl.Contains("undefined");
    }

    private void HandleFallbackConfiguration(WebApplicationBuilder builder, string? postgresConnectionString)
    {
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            Console.WriteLine("Using POSTGRES_CONNECTION_STRING from environment variables as fallback");
            builder.Configuration["ConnectionStrings:PostgreSQL"] = postgresConnectionString;
        }
        else
        {
            Console.WriteLine("WARNING: No PostgreSQL connection string available in environment variables.");
        }
    }

    private void ConfigureKeyVaultService(WebApplicationBuilder builder, string keyVaultUrl,
        string? postgresConnectionString)
    {
        try
        {
            var keyVaultService = new KeyVaultService(keyVaultUrl);

            if (!keyVaultService.IsAvailable)
            {
                Console.WriteLine("Key Vault service is not available - using fallback configuration");
                HandleFallbackConfiguration(builder, postgresConnectionString);
                return;
            }

            builder.Services.AddSingleton<IKeyVaultService>(keyVaultService);
            Console.WriteLine($"Connected to Azure Key Vault: {keyVaultUrl}");

            // Retrieve secrets
            RetrieveSecrets(keyVaultService, builder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Error connecting to Key Vault: {ex.Message}");
            Console.WriteLine("Using environment variables directly if available");
            HandleFallbackConfiguration(builder, postgresConnectionString);
        }

        ValidateConfiguration(builder);
    }

    private void RetrieveSecrets(IKeyVaultService keyVaultService, WebApplicationBuilder builder)
    {
        if (string.IsNullOrEmpty(builder.Configuration["ConnectionStrings:PostgreSQL"]))
        {
            TryStoreSecret(keyVaultService, "PostgresConnectionString",
                new[] { "ConnectionStrings:PostgreSQL" }, builder);
        }

        TryStoreSecret(keyVaultService, "BlobConnectionString",
            new[] { "BlobStorageSettings:ConnectionString", "BlobConnectionString" }, builder);

        // Add Review API secrets
        TryStoreSecret(keyVaultService, "ReviewApiURL", new[] { "ReviewApiURL" }, builder);
        TryStoreSecret(keyVaultService, "ReviewApiKey", new[] { "ReviewApiKey" }, builder);
        TryStoreSecret(keyVaultService, "MockReviewApiURL", new[] { "MockReviewApiURL" }, builder);
        TryStoreSecret(keyVaultService, "MockReviewApiKey", new[] { "MockReviewApiKey" }, builder);
    }

    private void TryStoreSecret(IKeyVaultService service, string secretName, string[] configKeys,
        WebApplicationBuilder builder)
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

    private void ValidateConfiguration(WebApplicationBuilder builder)
    {
        if (string.IsNullOrEmpty(builder.Configuration["ConnectionStrings:PostgreSQL"]))
        {
            Console.WriteLine("CRITICAL: No PostgreSQL connection string available after all attempts!");
        }
        else
        {
            Console.WriteLine("PostgreSQL connection string is configured (value not shown for security reasons)");
        }
    }
}