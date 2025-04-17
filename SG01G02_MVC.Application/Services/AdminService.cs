using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Application.Services
{
    /// Temporary in-memory login validation.
    /// TODO: Replace hardcoded credentials with real authentication (Entra ID, secure DB, etc.)
    public class AdminService : IAdminService
    {
        public bool ValidateLogin(string username, string password)
        {
            return username == "admin" && password == "securepassword123";
        }
    }
}