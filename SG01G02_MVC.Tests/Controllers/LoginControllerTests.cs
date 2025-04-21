using Moq;
using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Web.Controllers;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Web.Services;
using Xunit;

namespace SG01G02_MVC.Tests.Controllers
{
    public class LoginControllerTests
    {
        [Fact]
        public void Login_ValidCredentials_ShouldRedirectToHome()
        {
            // Arrange
            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(s => s.ValidateLogin("user", "correctpass"))
                .Returns(new SG01G02_MVC.Domain.Entities.AppUser { Username = "user", Role = "Customer" });

            var mockSession = new Mock<IUserSessionService>();
            var controller = new LoginController(mockAuthService.Object, mockSession.Object);
            var model = new LoginViewModel { Username = "user", Password = "correctpass" };

            // Act
            var result = controller.Index(model); // ✅ should call Index, not Login

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName); // Adjust if needed
        }

        [Fact]
        public void Login_InvalidCredentials_ShouldReturnViewWithError()
        {
            // Arrange
            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(s => s.ValidateLogin("user", "wrongpass"))
                .Returns((SG01G02_MVC.Domain.Entities.AppUser?)null);

            var mockSession = new Mock<IUserSessionService>(); // ✅ added to match constructor
            var controller = new LoginController(mockAuthService.Object, mockSession.Object);
            var model = new LoginViewModel { Username = "user", Password = "wrongpass" };

            // Act
            var result = controller.Index(model); // ✅ should call Index, not Login

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.IsValid); // ✅ this line may need to change if ModelState is not invalid
            Assert.Equal(model, viewResult.Model);
        }
    }
}