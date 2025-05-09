using Moq;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Infrastructure.External;

namespace SG01G02_MVC.Tests.Services
{
    public class ReviewServiceTests
    {
        [Fact]
        public async Task GetReviewsForProduct_ReturnsReviewsFromApiClient()
        {
            // Arrange
            var mockApiClient = new Mock<ReviewApiClient>(null) { CallBase = true };
            var expectedReviews = new List<ReviewDto>
            {
                new ReviewDto { Id = "1", ProductId = "p1", Author = "Alice", Content = "Great!", Rating = 5 },
                new ReviewDto { Id = "2", ProductId = "p1", Author = "Bob", Content = "Okay", Rating = 3 }
            };
            mockApiClient.Setup(c => c.GetReviewsForProductAsync("p1"))
                        .ReturnsAsync(expectedReviews);

            var service = new ReviewService(mockApiClient.Object);

            // Act
            var result = await service.GetReviewsForProduct("p1");

            // Assert
            Assert.Equal(expectedReviews, result);
        }
    }
}