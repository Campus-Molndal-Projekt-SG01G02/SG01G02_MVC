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
        Mock<IReviewApiClient>? reviewApiClient = null,
        Mock<IFeatureToggleService>? featureToggleService = null)
    {
        var mockSession = sessionService ?? new Mock<IUserSessionService>();
        mockSession.Setup(s => s.Role).Returns("Admin"); // Default role is admin for testing, override if needed

        var mockProductService = productService ?? new Mock<IProductService>();
        var mockBlobService = blobStorageService ?? new Mock<IBlobStorageService>();
        var mockReviewApiClient = reviewApiClient ?? new Mock<IReviewApiClient>();
        var mockFeatureToggle = featureToggleService ?? new Mock<IFeatureToggleService>();

        // Setup basic behavior for blob service
        mockBlobService
            .Setup(b => b.GetBlobUrl(It.IsAny<string>()))
            .Returns<string>(blobName => $"https://fakeblob.example.com/{blobName}");

        // Setup default behavior for feature toggle (can be overridden in specific tests)
        mockFeatureToggle
            .Setup(f => f.UseMockReviewApi())
            .Returns(false); // Default to production mode

        var controller = new AdminController(
            mockProductService.Object,
            mockSession.Object,
            mockBlobService.Object,
            mockReviewApiClient.Object,
            mockFeatureToggle.Object
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
        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        var context = GetInMemoryDbContext();
        context.Database.EnsureCreated(); // Simulate DB being connectable

        var controller = new AdminController(
            mockProductService.Object, 
            mockSession.Object,
            mockBlobService.Object,
            new Mock<IReviewApiClient>().Object,
            mockFeatureToggle.Object
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
        var viewResult = Assert.IsType<ViewResult>(result);
        
        // Verify that ViewBag.UseMockApi is set
        Assert.NotNull(controller.ViewBag.UseMockApi);
        Assert.False((bool)controller.ViewBag.UseMockApi); // Default setup returns false
    }

    [Fact]
    public async Task Index_WithMockApiEnabled_ShouldSetViewBagCorrectly()
    {
        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        mockFeatureToggle.Setup(f => f.UseMockReviewApi()).Returns(true);
        
        var (controller, _) = CreateController(featureToggleService: mockFeatureToggle);

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
        var viewResult = Assert.IsType<ViewResult>(result);
        
        // Verify that ViewBag.UseMockApi is set to true
        Assert.NotNull(controller.ViewBag.UseMockApi);
        Assert.True((bool)controller.ViewBag.UseMockApi);
    }

    [Fact]
    public async Task Create_ValidProduct_RedirectsToIndex()
    {
        var mockReviewClient = new Mock<IReviewApiClient>();
        mockReviewClient
            .Setup(c => c.RegisterProductAsync(It.IsAny<ProductDto>()))
            .ReturnsAsync(123); // simulate success

        var (controller, mockService) = CreateController(reviewApiClient: mockReviewClient);
        var product = new ProductViewModel { Name = "Test", Price = 10 };

        var result = await controller.Create(product);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        mockService.Verify(s => s.CreateProductAsync(It.IsAny<ProductDto>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithMockApiEnabled_ShouldShowMockApiMessage()
    {
        var mockReviewClient = new Mock<IReviewApiClient>();
        mockReviewClient
            .Setup(c => c.RegisterProductAsync(It.IsAny<ProductDto>()))
            .ReturnsAsync(123); // simulate success

        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        mockFeatureToggle.Setup(f => f.UseMockReviewApi()).Returns(true);

        var (controller, mockService) = CreateController(
            reviewApiClient: mockReviewClient, 
            featureToggleService: mockFeatureToggle);
        
        var product = new ProductViewModel { Name = "Test", Price = 10 };

        var result = await controller.Create(product);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        
        // Verify that the mock API message was set
        Assert.NotNull(controller.TempData["ReviewInfo"]);
        Assert.Contains("Mock API", controller.TempData["ReviewInfo"].ToString());
    }

    [Fact]
    public async Task Create_WithMockApiDisabled_RegistrationFailure_ShouldShowErrorMessage()
    {
        var mockReviewClient = new Mock<IReviewApiClient>();
        mockReviewClient
            .Setup(c => c.RegisterProductAsync(It.IsAny<ProductDto>()))
            .ReturnsAsync((int?)null); // simulate failure

        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        mockFeatureToggle.Setup(f => f.UseMockReviewApi()).Returns(false);

        var (controller, mockService) = CreateController(
            reviewApiClient: mockReviewClient, 
            featureToggleService: mockFeatureToggle);
        
        var product = new ProductViewModel { Name = "Test", Price = 10 };

        var result = await controller.Create(product);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        
        // Verify that the error message was set
        Assert.NotNull(controller.TempData["ReviewError"]);
        Assert.Contains("External product registration failed", controller.TempData["ReviewError"].ToString());
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
        var mockReviewClient = new Mock<IReviewApiClient>();

        mockReviewClient
            .Setup(c => c.RegisterProductAsync(It.IsAny<ProductDto>()))
            .ReturnsAsync(123); // simulate success

        mockBlobService
            .Setup(b => b.UploadImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("test-image-filename.jpg");
            
        var (controller, mockService) = CreateController(mockProductService, null, mockBlobService, mockReviewClient);
        
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

    [Fact]
    public async Task Create_GET_ShouldSetFeatureToggleInViewBag()
    {
        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        mockFeatureToggle.Setup(f => f.UseMockReviewApi()).Returns(true);
        
        var (controller, _) = CreateController(featureToggleService: mockFeatureToggle);

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewBag.UseMockApi);
        Assert.True((bool)controller.ViewBag.UseMockApi);
    }

    [Fact]
    public async Task Edit_GET_ShouldSetFeatureToggleInViewBag()
    {
        var mockProductService = new Mock<IProductService>();
        mockProductService
            .Setup(s => s.GetProductByIdAsync(1))
            .ReturnsAsync(new ProductDto { Id = 1, Name = "Test Product", Price = 10 });

        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        mockFeatureToggle.Setup(f => f.UseMockReviewApi()).Returns(false);
        
        var (controller, _) = CreateController(mockProductService, featureToggleService: mockFeatureToggle);

        var result = await controller.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewBag.UseMockApi);
        Assert.False((bool)controller.ViewBag.UseMockApi);
    }
}