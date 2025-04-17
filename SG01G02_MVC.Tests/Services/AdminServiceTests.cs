using Xunit;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;

namespace SG01G02_MVC.Tests.Services
{
    public class AdminServiceTests
    {
        private readonly IAdminService _adminService;

        public AdminServiceTests()
        {
            _adminService = new AdminService();
        }

        [Theory]
        [InlineData("admin", "password123", true)]
        [InlineData("Admin", "password123", false)]
        [InlineData("admin", "wrongpassword", false)]
        [InlineData("invalid", "invalid", false)]
        public void ValidateLogin_ReturnsExpectedResult(string username, string password, bool expected)
        {
            // Act
            var result = _adminService.ValidateLogin(username, password);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}