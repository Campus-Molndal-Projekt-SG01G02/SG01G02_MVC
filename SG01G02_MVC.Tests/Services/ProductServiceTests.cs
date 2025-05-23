using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Application.Interfaces;
using Moq;

namespace SG01G02_MVC.Tests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnListOfProducts()
    {
        // Arrange
        var repo = new FakeProductRepository();
        var fakeReviewApiClient = new Mock<IReviewApiClient>().Object;
        var service = new ProductService(repo, fakeReviewApiClient);

        // Act
        var result = await service.GetAllProductsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<ProductDto>>(result);
    }
}