using SG01G02_MVC.Domain.Entities;

namespace SG01G02_MVC.Application.Interfaces
{
    public interface IUserRepository
    {
        AppUser? GetByUsername(string username);

        bool ValidatePassword(AppUser user, string plainTextPassword);
    }
}