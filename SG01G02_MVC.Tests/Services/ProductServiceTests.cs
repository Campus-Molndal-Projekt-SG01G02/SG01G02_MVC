using Xunit;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Domain.Entities;
using System.Collections.Generic;
using SG01G02_MVC.Tests.Services;

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
            Assert.IsType<List<Product>>(result);
        }
    }
}