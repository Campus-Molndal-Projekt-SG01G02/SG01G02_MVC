namespace SG01G02_MVC.Web.Services;

public interface IUserSessionService
{
    string? Username { get; set; }
    string? Role { get; set; }
    void Clear();
}