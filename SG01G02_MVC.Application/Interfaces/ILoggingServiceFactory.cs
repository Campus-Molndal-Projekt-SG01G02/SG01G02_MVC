namespace SG01G02_MVC.Application.Interfaces;

public interface ILoggingServiceFactory
{
    ILoggingService Create(IKeyVaultService? keyVaultService);
}