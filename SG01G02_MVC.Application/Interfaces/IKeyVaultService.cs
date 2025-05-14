namespace SG01G02_MVC.Application.Interfaces;

public interface IKeyVaultService
{
    string GetSecret(string secretName);
    bool IsAvailable { get; }
}