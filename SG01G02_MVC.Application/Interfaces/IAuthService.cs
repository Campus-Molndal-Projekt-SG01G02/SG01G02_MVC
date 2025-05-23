using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Interfaces;

public interface IAuthService
{
    AppUser? ValidateLogin(string username, string password);
}