using System.Threading.Tasks;
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
        public async Task Login_ValidCredentials_ShouldRedirectToHome()
        {
            // Arrange
            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(s => s.ValidateLogin("user", "correctpass"))
                .Returns(new SG01G02_MVC.Domain.Entities.AppUser { Username = "user", Role = "Customer" });

            var mockSession = new Mock<IUserSessionService>();
            var controller = new LoginController(mockAuthService.Object, mockSession.Object);
            SetupHttpContextForAuth(controller);
            var model = new LoginViewModel { Username = "user", Password = "correctpass" };

            // Act
            var result = await controller.Index(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Home", redirectResult.ControllerName);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ShouldReturnViewWithError()
        {
            // Arrange
            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(s => s.ValidateLogin("user", "wrongpass"))
                .Returns((SG01G02_MVC.Domain.Entities.AppUser?)null);

            var mockSession = new Mock<IUserSessionService>();
            var controller = new LoginController(mockAuthService.Object, mockSession.Object);
            var model = new LoginViewModel { Username = "user", Password = "wrongpass" };

            // Act
            var result = await controller.Index(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.True(controller.ModelState.IsValid); // Not required, but doesn't hurt
        }

        private void SetupHttpContextForAuth(LoginController controller)
        {
            var httpContext = new DefaultHttpContext();
            var authServiceMock = new Mock<IAuthenticationService>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            httpContext.RequestServices = serviceProviderMock.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            authServiceMock
                .Setup(x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);
        }
    }
}