using Xunit;
using SG01G02_MVC.Application.Services;
using SG01G02_MVC.Domain.Entities;
using System.Collections.Generic;

namespace SG01G02_MVC.Tests.Services
{
    public class ProductServiceTests
    {
        [Fact]
        public void GetAllProducts_ShouldReturnListOfProducts()
        {
            // Arrange
            var service = new ProductService();

            // Act
            var result = service.GetAllProducts();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Product>>(result);
        }
    }
}