using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Web.Controllers;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Web.Models;

namespace SG01G02_MVC.Tests.Controllers
{
    public class CatalogueControllerTests
    {
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfProducts()
        {
            // Arrange
            var mockProductService = new Mock<IProductService>();
            var mockReviewService = new Mock<IReviewService>();
            var mockBlobService = new Mock<IBlobStorageService>();

            mockProductService.Setup(s => s.GetAllProductsAsync())
                .ReturnsAsync(new List<SG01G02_MVC.Application.DTOs.ProductDto>
                {
                    new SG01G02_MVC.Application.DTOs.ProductDto { Id = 1, Name = "Test Product", Price = 10 }
                });

            mockReviewService.Setup(s => s.GetReviewsForProduct(It.IsAny<int>()))
                .ReturnsAsync(new List<SG01G02_MVC.Application.DTOs.ReviewDto>());

            var controller = new CatalogueController(
                mockProductService.Object,
                mockReviewService.Object,
                mockBlobService.Object
            );

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ProductViewModel>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Test Product", model[0].Name);
        }

        [Fact]
        public async Task Details_ProductExists_ReturnsViewResultWithProduct()
        {
            // Arrange
            var mockProductService = new Mock<IProductService>();
            var mockReviewService = new Mock<IReviewService>();
            var mockBlobService = new Mock<IBlobStorageService>();

            mockProductService.Setup(s => s.GetProductByIdAsync(1))
                .ReturnsAsync(new SG01G02_MVC.Application.DTOs.ProductDto { Id = 1, Name = "Test Product", Price = 10 });

            mockReviewService.Setup(s => s.GetReviewsForProduct(It.IsAny<int>()))
                .ReturnsAsync(new List<SG01G02_MVC.Application.DTOs.ReviewDto>());

            var controller = new CatalogueController(
                mockProductService.Object,
                mockReviewService.Object,
                mockBlobService.Object
            );

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProductViewModel>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Test Product", model.Name);
        }

        [Fact]
        public async Task Details_ProductDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var mockProductService = new Mock<IProductService>();
            var mockReviewService = new Mock<IReviewService>();
            var mockBlobService = new Mock<IBlobStorageService>();

            mockProductService.Setup(s => s.GetProductByIdAsync(99))
                .ReturnsAsync((SG01G02_MVC.Application.DTOs.ProductDto?)null);

            var controller = new CatalogueController(
                mockProductService.Object,
                mockReviewService.Object,
                mockBlobService.Object
            );

            // Act
            var result = await controller.Details(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
