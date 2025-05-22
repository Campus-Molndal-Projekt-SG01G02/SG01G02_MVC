using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Tests.Helpers;
using SG01G02_MVC.Web.Controllers;
using SG01G02_MVC.Web.Models;
using SG01G02_MVC.Web.Services;
using SG01G02_MVC.Infrastructure.Services;

namespace SG01G02_MVC.Tests.Controllers;
public class AdminControllerTests : TestBase
{
    // Helper method to create a controller with mocked dependencies
    private (AdminController controller, Mock<IProductService> mockService) CreateController(
        Mock<IProductService>? productService = null,
        Mock<IUserSessionService>? sessionService = null,
        Mock<IBlobStorageService>? blobStorageService = null,
        Mock<IReviewApiClient>? reviewApiClient = null)
    {
        var mockSession = sessionService ?? new Mock<IUserSessionService>();
        mockSession.Setup(s => s.Role).Returns("Admin"); // Default role is admin for testing, override if needed

        var mockProductService = productService ?? new Mock<IProductService>();
        var mockBlobService = blobStorageService ?? new Mock<IBlobStorageService>();
        var mockReviewApiClient = reviewApiClient ?? new Mock<IReviewApiClient>();

        // Setup basic behavior för blob service
        mockBlobService
            .Setup(b => b.GetBlobUrl(It.IsAny<string>()))
            .Returns<string>(blobName => $"https://fakeblob.example.com/{blobName}");

        var controller = new AdminController(
            mockProductService.Object,
            mockSession.Object,
            mockBlobService.Object,
            mockReviewApiClient.Object
        );

        return (controller, mockProductService);
    }

    [Fact]
    public async Task Index_UnauthenticatedUser_ShouldRedirectToLogin()
    {
        var mockSession = new Mock<IUserSessionService>();
        mockSession.Setup(s => s.Role).Returns("Customer"); // Not admin

        var mockProductService = new Mock<IProductService>();
        var mockBlobService = new Mock<IBlobStorageService>();
        var context = GetInMemoryDbContext();
        context.Database.EnsureCreated(); // Simulate DB being connectable

        var controller = new AdminController(
            mockProductService.Object, 
            mockSession.Object,
            mockBlobService.Object,
            new Mock<IReviewApiClient>().Object
        );

        // Simulate authenticated user
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "notadmin"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        var result = await controller.Index();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Login", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Index_AuthenticatedAdminUser_ShouldReturnView()
    {
        var (controller, _) = CreateController();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock");

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var result = await controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_ValidProduct_RedirectsToIndex()
    {
        var (controller, mockService) = CreateController();
        var product = new ProductViewModel { Name = "Test", Price = 10 };

        var result = await controller.Create(product);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        mockService.Verify(s => s.CreateProductAsync(It.IsAny<ProductDto>()), Times.Once);
    }

    [Fact]
    public async Task Edit_ValidProduct_RedirectsToIndex()
    {
        // Setup mockProductService with GetProductByIdAsync
        var mockProductService = new Mock<IProductService>();
        mockProductService
            .Setup(s => s.GetProductByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new ProductDto { Id = 1, Name = "Original", Price = 10 });
            
        var (controller, mockService) = CreateController(mockProductService);
        var product = new ProductViewModel { Id = 1, Name = "Updated", Price = 15 };

        var result = await controller.Edit(product);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        mockService.Verify(s => s.UpdateProductAsync(It.IsAny<ProductDto>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Confirmed_DeletesProductAndRedirects()
    {
        // Setup mockProductService to return a product when GetProductByIdAsync is called
        var mockProductService = new Mock<IProductService>();
        mockProductService
            .Setup(s => s.GetProductByIdAsync(1))
            .ReturnsAsync(new ProductDto { Id = 1, Name = "Test Product" });
            
        var (controller, mockService) = CreateController(mockProductService);

        var result = await controller.DeleteConfirmed(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        mockService.Verify(s => s.DeleteProductAsync(1), Times.Once);
    }
    
    [Fact]
    public async Task Create_WithImageFile_UploadsImageAndRedirectsToIndex()
    {
        // Setup mocks
        var mockProductService = new Mock<IProductService>();
        var mockBlobService = new Mock<IBlobStorageService>();
        
        // Setup the blob service to return a filename when UploadImageAsync is called
        mockBlobService
            .Setup(b => b.UploadImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("test-image-filename.jpg");
            
        var (controller, mockService) = CreateController(mockProductService, null, mockBlobService);
        
        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024); // Non-empty file
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        
        // Create the product viewmodel with the image file
        var product = new ProductViewModel 
        { 
            Name = "Test", 
            Price = 10,
            ImageFile = fileMock.Object
        };

        var result = await controller.Create(product);

        // Verify the expected calls were made
        mockBlobService.Verify(b => b.UploadImageAsync(It.IsAny<IFormFile>()), Times.Once);
        mockService.Verify(s => s.CreateProductAsync(It.Is<ProductDto>(p => p.ImageName == "test-image-filename.jpg")), Times.Once);
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}