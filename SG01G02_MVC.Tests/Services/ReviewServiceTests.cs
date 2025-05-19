using Moq;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Net.Http;

namespace SG01G02_MVC.Tests.Services
{
    public class ReviewServiceTests
    {
        private readonly Mock<IReviewApiClient> _mockReviewApiClient;
        private readonly Mock<ILogger<ReviewService>> _mockLogger;
        private readonly ReviewService _reviewService;

        public ReviewServiceTests()
        {
            _mockReviewApiClient = new Mock<IReviewApiClient>();
            _mockLogger = new Mock<ILogger<ReviewService>>();
            _reviewService = new ReviewService(_mockReviewApiClient.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetReviewsForProduct_ReturnsReviewsFromApiClient()
        {
            // Arrange
            var expectedReviews = new List<ReviewDto>
            {
                new ReviewDto {
                    Id = "1",
                    ProductId = "productId",
                    CustomerName = "Alice",
                    Content = "Great product!",
                    Rating = 5,
                    CreatedAt = DateTime.UtcNow,
                    Status = "approved"
                },
                new ReviewDto {
                    Id = "2",
                    ProductId = "productId",
                    CustomerName = "Bob",
                    Content = "Not bad.",
                    Rating = 4,
                    CreatedAt = DateTime.UtcNow,
                    Status = "approved"
                }
            };
            _mockReviewApiClient.Setup(client => client.GetReviewsAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedReviews);

            // Act
            var reviews = await _reviewService.GetReviewsForProduct("productId");

            // Assert
            Assert.NotNull(reviews);
            Assert.Equal(2, reviews.Count());
            Assert.Contains(reviews, r => r.Content == "Great product!");
            Assert.Contains(reviews, r => r.Content == "Not bad.");
        }

        [Fact]
        public async Task GetReviewsForProduct_EmptyProductId_ThrowsArgumentException()
        {
            // Arrange
            string emptyProductId = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _reviewService.GetReviewsForProduct(emptyProductId));
        }

        [Fact]
        public async Task GetReviewsForProduct_NullProductId_ThrowsArgumentException()
        {
            // Arrange
            string? nullProductId = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _reviewService.GetReviewsForProduct(nullProductId!));
        }

        [Fact]
        public async Task GetReviewsForProduct_ApiClientReturnsEmptyList_ReturnsEmptyList()
        {
            // Arrange
            _mockReviewApiClient.Setup(client => client.GetReviewsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<ReviewDto>());

            // Act
            var reviews = await _reviewService.GetReviewsForProduct("productId");

            // Assert
            Assert.NotNull(reviews);
            Assert.Empty(reviews);
        }
    }
}