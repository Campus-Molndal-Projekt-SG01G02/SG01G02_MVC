namespace SG01G02_MVC.Application.Interfaces
{
    public interface IAdminService
    {
        bool ValidateLogin(string username, string password);
    }
}