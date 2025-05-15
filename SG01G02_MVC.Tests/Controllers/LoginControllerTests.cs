using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using SG01G02_MVC.Web.Controllers;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Routing;

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
            var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
            var urlHelperMock = new Mock<IUrlHelper>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            // Setup URL helper mock
            urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("mockedUrl");

            // Setup URL helper factory mock
            urlHelperFactoryMock
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelperMock.Object);

            // Setup service provider to return both auth service and URL helper factory
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IUrlHelperFactory)))
                .Returns(urlHelperFactoryMock.Object);

            httpContext.RequestServices = serviceProviderMock.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Set URL helper directly
            controller.Url = urlHelperMock.Object;

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