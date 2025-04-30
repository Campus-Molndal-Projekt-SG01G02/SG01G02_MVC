using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Tests.Helpers;
using SG01G02_MVC.Web.Controllers;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Web.Services;

namespace SG01G02_MVC.Tests.Controllers
{
    public class AdminControllerTests : TestBase
    {
        // Helper method to create a controller with mocked dependencies
        private (AdminController controller, Mock<IProductService> mockService) CreateController(Mock<IProductService>? productService = null,
            Mock<IUserSessionService>? sessionService = null)
        {
            var mockSession = sessionService ?? new Mock<IUserSessionService>();
            mockSession.Setup(s => s.Role).Returns("Admin"); // Default role is admin for testing, override if needed

            var mockProductService = productService ?? new Mock<IProductService>();
            var context = GetInMemoryDbContext();

            var controller = new AdminController(
                mockProductService.Object,
                context,
                mockSession.Object
            );

            return (controller, mockProductService);
        }

        [Fact]
        public async Task Index_UnauthenticatedUser_ShouldRedirectToLogin()
        {
            var mockSession = new Mock<IUserSessionService>();
            mockSession.Setup(s => s.Role).Returns("Customer"); // Not admin

            var context = GetInMemoryDbContext();
            context.Database.EnsureCreated(); // ✅ Simulate DB being connectable

            var mockProductService = new Mock<IProductService>();
            var controller = new AdminController(mockProductService.Object, context, mockSession.Object);

            // Simulate authenticated user
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "notadmin"),
                new Claim(ClaimTypes.Role, "Customer")
            }, "mock");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            var result = await controller.Index();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Login", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Index_AuthenticatedAdminUser_ShouldReturnView()
        {
            var (controller, _) = CreateController();

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock");

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            var result = await controller.Index(); // ✅ add await
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_ValidProduct_RedirectsToIndex()
        {
            var (controller, mockService) = CreateController();
            var product = new ProductViewModel { Name = "Test", Price = 10 };

            var result = await controller.AddProduct(product);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.CreateProductAsync(It.IsAny<ProductDto>()), Times.Once);
        }

        [Fact]
        public async Task Edit_ValidProduct_RedirectsToIndex()
        {
            var (controller, mockService) = CreateController();
            var product = new ProductViewModel { Id = 1, Name = "Updated", Price = 15 };

            var result = await controller.EditProduct(1, product);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.UpdateProductAsync(It.IsAny<ProductDto>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Confirmed_DeletesProductAndRedirects()
        {
            var (controller, mockService) = CreateController();

            var result = await controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.DeleteProductAsync(1), Times.Once);
        }
    }
}