using Moq;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.Services;

namespace SG01G02_MVC.Tests.Services
{
    public class ReviewServiceTests
    {
        [Fact]
        public async Task GetReviewsForProduct_ReturnsReviewsFromApiClient()
        {
            // Arrange
            var mockReviewApiClient = new Mock<IReviewApiClient>();
            mockReviewApiClient.Setup(client => client.GetReviewsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<ReviewDto>
                {
                    new ReviewDto { Id = "1", Content = "Great product!" },
                    new ReviewDto { Id = "2", Content = "Not bad." }
                });

            var reviewService = new ReviewService(mockReviewApiClient.Object);

            // Act
            var reviews = await reviewService.GetReviewsForProduct("productId");

            // Assert
            Assert.NotNull(reviews);
            Assert.Equal(2, reviews.Count());
            Assert.Contains(reviews, r => r.Content == "Great product!");
        }
    }
}