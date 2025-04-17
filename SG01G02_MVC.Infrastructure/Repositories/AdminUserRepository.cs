using SG01G02_MVC.Application.Interfaces;

namespace SG01G02_MVC.Infrastructure.Repositories
{
    /// Stub repository for Admin authentication.
    ///
    /// TODO: Replace this implementation with secure authentication mechanism:
    /// - Entra ID (Azure AD)
    /// - ASP.NET Identity
    /// - Or database-backed admin user store
    ///
    /// Current version is MVP-only with hardcoded credentials.
    public class AdminUserRepository : IAdminService
    {
        public bool ValidateLogin(string username, string password)
        {
            return username == "admin" && password == "securepassword123";
        }
    }
}