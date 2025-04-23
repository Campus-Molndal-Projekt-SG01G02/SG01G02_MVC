using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SG01G02_MVC.Web.Controllers;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Application.Interfaces;
using System.Security.Claims;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Tests.Helpers;
using SG01G02_MVC.Web.Services;

namespace SG01G02_MVC.Tests.Controllers
{
    public class AdminControllerTests : TestBase
    {
        private AdminController CreateController(Mock<IProductService> productService = null)
        {
            var mockSession = new Mock<IUserSessionService>();
            var context = GetInMemoryDbContext();
            return new AdminController(
                productService?.Object ?? new Mock<IProductService>().Object,
                context,
                mockSession.Object
            );
        }

        [Fact]
        public void Index_UnauthenticatedUser_ShouldRedirectToLogin()
        {
            var mockService = new Mock<IProductService>();
            var controller = CreateController(mockService);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() // No user = not authenticated
            };

            var result = controller.Index();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Login", redirectResult.ControllerName);
        }

        [Fact]
        public void Index_AuthenticatedAdminUser_ShouldReturnView()
        {
            var mockService = new Mock<IProductService>();
            var controller = CreateController(mockService);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock");

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_ValidProduct_RedirectsToIndex()
        {
            var mockService = new Mock<IProductService>();
            var controller = CreateController(mockService);
            var product = new ProductViewModel { Name = "Test", Price = 10 };

            var result = await controller.Create(product);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.CreateProductAsync(It.IsAny<ProductDto>()), Times.Once);
        }

        [Fact]
        public async Task Edit_ValidProduct_RedirectsToIndex()
        {
            var mockService = new Mock<IProductService>();
            var controller = CreateController(mockService);
            var product = new ProductViewModel { Id = 1, Name = "Updated", Price = 15 };

            var result = await controller.Edit(1, product);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.UpdateProductAsync(It.IsAny<ProductDto>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Confirmed_DeletesProductAndRedirects()
        {
            var mockService = new Mock<IProductService>();
            var controller = CreateController(mockService);

            var result = await controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.DeleteProductAsync(1), Times.Once);
        }
    }
}