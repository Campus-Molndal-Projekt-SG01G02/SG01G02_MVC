using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Services;

public class KeyVaultService : IKeyVaultService
{
    
    private readonly SecretClient _secretClient = null!;
    private bool _isAvailable;

    public bool IsAvailable => _isAvailable;

    public KeyVaultService(string keyVaultUrl)
    {
        // Print the value for debugging
        Console.WriteLine($"Original Key Vault URL: '{keyVaultUrl}'");

        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            Console.WriteLine("Key Vault URL is null or empty. Checking KEY_VAULT_NAME environment variable...");

            var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
            Console.WriteLine($"KEY_VAULT_NAME environment variable: '{keyVaultName}'");

            if (!string.IsNullOrEmpty(keyVaultName) &&
                !keyVaultName.Contains("your-key-vault-name") &&
                !keyVaultName.Contains("${"))
            {
                keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
                Console.WriteLine($"Generated Key Vault URL from environment variable: {keyVaultUrl}");
            }
            else
            {
                Console.WriteLine("Could not find a valid Key Vault name in environment variable.");
                _isAvailable = false;
                return;
            }
        }

        // Check if URL contains placeholder values
        if (keyVaultUrl.Contains("your-key-vault-name") ||
            keyVaultUrl.Contains("${") ||
            keyVaultUrl.Contains("undefined"))
        {
            Console.WriteLine($"WARNING: Invalid Key Vault URL format: {keyVaultUrl}");
            _isAvailable = false;
            return;
        }

        try
        {
            Console.WriteLine($"Attempting to connect to Key Vault at: {keyVaultUrl}");

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(
                    new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            });

            Console.WriteLine("Testing Azure authentication...");
            var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://vault.azure.net/.default" });

            try
            {
                var token = credential.GetToken(tokenRequestContext);

                if (!string.IsNullOrEmpty(token.Token))
                {
                    Console.WriteLine("Azure authentication succeeded!");
                    _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
                    _isAvailable = true;
                }
                else
                {
                    Console.WriteLine("Azure authentication failed: Could not retrieve token");
                    _isAvailable = false;
                }
            }
            catch (Azure.Identity.AuthenticationFailedException authEx)
            {
                Console.WriteLine($"Azure authentication failed: {authEx.Message}");
                Console.WriteLine($"Inner exception: {authEx.InnerException?.Message}");
                _isAvailable = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to Azure Key Vault: {ex.Message}");
            Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            _isAvailable = false;
        }
    }

    public string GetSecret(string secretName)
    {
        if (!_isAvailable)
        {
            throw new InvalidOperationException("Key Vault service is not available.");
        }

        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentNullException(nameof(secretName), "Secret name must not be empty.");
        }

        try
        {
            Console.WriteLine($"Retrieving secret: {secretName}");
            var secret = _secretClient.GetSecret(secretName);
            Console.WriteLine($"Successfully retrieved secret: {secretName}");
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving secret '{secretName}': {ex.Message}");
            throw new Exception($"Error retrieving secret '{secretName}' from Key Vault: {ex.Message}", ex);
        }
    }
}
