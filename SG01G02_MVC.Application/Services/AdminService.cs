using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Application.Services
{
    public class AdminService : IAdminService
    {
        // Hardcoded for MVP phase only
        private const string ValidUsername = "admin";
        private const string ValidPassword = "password123";

        public bool ValidateLogin(string username, string password)
        {
            return username == ValidUsername && password == ValidPassword;
        }
    }
}