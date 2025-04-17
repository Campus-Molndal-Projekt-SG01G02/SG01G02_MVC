namespace SG01G02_MVC.Domain.Entities
{
    /// Represents an application user for login/authentication.
    /// Role can be Admin, Staff, Customer, etc.
    public class AppUser
    {
        public int Id { get; set; }

        public string Username { get; set; } = default!;

        public string PasswordHash { get; set; } = default!;

        public string Role { get; set; } = "Customer"; // "Admin", "Staff", "Customer"
    }
}