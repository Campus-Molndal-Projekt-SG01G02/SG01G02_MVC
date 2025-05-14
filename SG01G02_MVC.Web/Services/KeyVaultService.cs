using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Web.Services;

public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _secretClient;
    private bool _isAvailable;

    public bool IsAvailable => _isAvailable;

    public KeyVaultService(string keyVaultUrl)
    {
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            throw new ArgumentNullException(nameof(keyVaultUrl), "Key Vault URL is required.");
        }

        try
        {
            _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            
            // Verifiera att vi kan ansluta till Key Vault
            var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://vault.azure.net/.default" });
            var credential = new DefaultAzureCredential();
            var token = credential.GetToken(tokenRequestContext);
            
            _isAvailable = !string.IsNullOrEmpty(token.Token);
        }
        catch
        {
            _isAvailable = false;
            throw;
        }
    }

    public string GetSecret(string secretName)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentNullException(nameof(secretName), "Secret name is required.");
        }

        try
        {
            var secret = _secretClient.GetSecret(secretName);
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving secret '{secretName}' from Key Vault: {ex.Message}", ex);
        }
    }
}
