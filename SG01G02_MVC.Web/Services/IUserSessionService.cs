namespace SG01G02_MVC.Web.Services
{
    // This is a mock interface for testing purposes.
    public interface IUserSessionService
    {
        string? Username { get; set; }
        string? Role { get; set; }
        void Clear();
    }
}