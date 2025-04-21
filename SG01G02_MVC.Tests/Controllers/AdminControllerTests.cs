using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SG01G02_MVC.Web.Controllers;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Application.Interfaces;
using System.Security.Claims;
using SG01G02_MVC.Application.DTOs;

namespace SG01G02_MVC.Tests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public void Index_UnauthenticatedUser_ShouldRedirectToLogin()
        {
            // Arrange
            var mockProductService = new Mock<IProductService>();
            var controller = new AdminController(mockProductService.Object);

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() // No user = not authenticated
            };

            // Act
            var result = controller.Index();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Login", redirectResult.ControllerName);
        }

        [Fact]
        public void Index_AuthenticatedAdminUser_ShouldReturnView()
        {
            // Arrange
            var mockProductService = new Mock<IProductService>();
            var controller = new AdminController(mockProductService.Object);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock");

            var user = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // Act
            var result = controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_ValidProduct_RedirectsToIndex()
        {
            // Arrange
            var mockService = new Mock<IProductService>();
            var controller = new AdminController(mockService.Object);
            var product = new ProductViewModel { Name = "Test", Price = 10 };

            // Act
            var result = await controller.Create(product);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.CreateProductAsync(It.IsAny<ProductDto>()), Times.Once);
        }

        [Fact]
        public async Task Edit_ValidProduct_RedirectsToIndex()
        {
            var mockService = new Mock<IProductService>();
            var controller = new AdminController(mockService.Object);
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
            var controller = new AdminController(mockService.Object);

            var result = await controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            mockService.Verify(s => s.DeleteProductAsync(1), Times.Once);
        }
    }
}