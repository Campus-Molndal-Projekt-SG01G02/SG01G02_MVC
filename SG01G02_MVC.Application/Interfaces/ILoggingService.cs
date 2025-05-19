using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface ILoggingService
    {
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    }
}