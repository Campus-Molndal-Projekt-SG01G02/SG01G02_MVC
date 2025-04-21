using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Application.DTOs;

namespace SG01G02_MVC.Tests.Services
{
    public class ProductServiceTests
    {
        [Fact]
        public void GetAllProducts_ShouldReturnListOfProducts()
        {
            // Arrange
            var repo = new FakeProductRepository(); // Create a dummy in-memory test class
            var service = new ProductService(repo);

            // Act
            var result = service.GetAllProducts();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<ProductDto>>(result);
        }
    }
}