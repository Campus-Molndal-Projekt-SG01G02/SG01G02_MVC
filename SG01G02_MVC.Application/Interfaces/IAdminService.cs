namespace SG01G02_MVC.Application.Interfaces
{
    /// Defines authentication logic for Admin user.
    /// TODO: Replace with real identity provider (e.g., Entra ID or ASP.NET Identity)
    public interface IAdminService
    {
        bool ValidateLogin(string username, string password);
    }
}