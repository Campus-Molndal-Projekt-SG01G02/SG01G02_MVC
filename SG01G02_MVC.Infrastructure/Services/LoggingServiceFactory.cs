using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Services;

public class LoggingServiceFactory : ILoggingServiceFactory
{
    public ILoggingService Create(IKeyVaultService? keyVaultService)
    {
        return new LoggingService(keyVaultService);
    }
}