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
        // Skriv ut värdet för felsökning
        Console.WriteLine($"Original Key Vault URL: '{keyVaultUrl}'");

        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            Console.WriteLine("Key Vault URL är null eller tom. Kontrollerar KEY_VAULT_NAME miljövariabel...");

            var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
            Console.WriteLine($"KEY_VAULT_NAME miljövariabel: '{keyVaultName}'");

            if (!string.IsNullOrEmpty(keyVaultName) &&
                !keyVaultName.Contains("your-key-vault-name") &&
                !keyVaultName.Contains("${"))
            {
                keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
                Console.WriteLine($"Genererade Key Vault URL från miljövariabel: {keyVaultUrl}");
            }
            else
            {
                Console.WriteLine("Kunde inte hitta giltigt Key Vault-namn i miljövariabel.");
                _isAvailable = false;
                return;
            }
        }

        // Kontrollera om URL innehåller platshållarvärden
        if (keyVaultUrl.Contains("your-key-vault-name") ||
            keyVaultUrl.Contains("${") ||
            keyVaultUrl.Contains("undefined"))
        {
            Console.WriteLine($"VARNING: Ogiltigt Key Vault URL-format: {keyVaultUrl}");
            _isAvailable = false;
            return;
        }

        try
        {
            Console.WriteLine($"Försöker ansluta till Key Vault på: {keyVaultUrl}");

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(
                    new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            });

            Console.WriteLine("Testar Azure-autentisering...");
            var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://vault.azure.net/.default" });

            try
            {
                var token = credential.GetToken(tokenRequestContext);

                if (!string.IsNullOrEmpty(token.Token))
                {
                    Console.WriteLine("Azure-autentisering lyckades!");
                    _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
                    _isAvailable = true;
                }
                else
                {
                    Console.WriteLine("Azure-autentisering misslyckades: Kunde inte hämta token");
                    _isAvailable = false;
                }
            }
            catch (Azure.Identity.AuthenticationFailedException authEx)
            {
                Console.WriteLine($"Azure-autentisering misslyckades: {authEx.Message}");
                Console.WriteLine($"Inner exception: {authEx.InnerException?.Message}");
                _isAvailable = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel vid anslutning till Azure Key Vault: {ex.Message}");
            Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            _isAvailable = false;
        }
    }

    public string GetSecret(string secretName)
    {
        if (!_isAvailable)
        {
            throw new InvalidOperationException("Key Vault-tjänsten är inte tillgänglig.");
        }

        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentNullException(nameof(secretName), "Secret-namnet får inte vara tomt.");
        }

        try
        {
            Console.WriteLine($"Hämtar hemlighet: {secretName}");
            var secret = _secretClient.GetSecret(secretName);
            Console.WriteLine($"Lyckades hämta hemlighet: {secretName}");
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel vid hämtning av hemlighet '{secretName}': {ex.Message}");
            throw new Exception($"Fel vid hämtning av hemlighet '{secretName}' från Key Vault: {ex.Message}", ex);
        }
    }
}
