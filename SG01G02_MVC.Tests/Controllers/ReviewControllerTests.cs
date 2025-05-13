using Microsoft.AspNetCore.Mvc;
using Moq;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Web.Controllers;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SG01G02_MVC.Tests.Controllers
{
    public class ReviewControllerTests
    {
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ILogger<ReviewController>> _mockLogger;
        private readonly ReviewController _controller;

        public ReviewControllerTests()
        {
            _mockReviewService = new Mock<IReviewService>();
            _mockLogger = new Mock<ILogger<ReviewController>>();
            _controller = new ReviewController(_mockReviewService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetProductReviews_ValidProductId_ReturnsReviews()
        {
            // Arrange
            var expectedReviews = new List<ReviewDto>
            {
                new ReviewDto { Id = "1", Content = "Great product!" },
                new ReviewDto { Id = "2", Content = "Not bad." }
            };
            _mockReviewService.Setup(service => service.GetReviewsForProduct(It.IsAny<string>()))
                .ReturnsAsync(expectedReviews);

            // Act
            var result = await _controller.GetProductReviews("productId");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var json = JsonConvert.SerializeObject(jsonResult.Value);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.True((bool)dict["success"]);
            Assert.NotNull(dict["reviews"]);
        }

        [Fact]
        public async Task GetProductReviews_InvalidProductId_ReturnsBadRequest()
        {
            // Arrange
            _mockReviewService.Setup(service => service.GetReviewsForProduct(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid product ID"));

            // Act
            var result = await _controller.GetProductReviews("invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            var json = JsonConvert.SerializeObject(badRequestResult.Value);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.False((bool)dict["success"]);
            Assert.Equal("Invalid product ID", dict["message"]);
        }

        [Fact]
        public async Task GetProductReviews_ServiceThrowsException_ReturnsServerError()
        {
            // Arrange
            _mockReviewService.Setup(service => service.GetReviewsForProduct(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetProductReviews("productId");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            var json = JsonConvert.SerializeObject(statusCodeResult.Value);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.False((bool)dict["success"]);
            Assert.Equal("An error occurred while fetching reviews.", dict["message"]);
        }
    }
} 