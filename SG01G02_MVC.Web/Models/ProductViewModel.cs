using System.ComponentModel.DataAnnotations;

namespace SG01G02_MVC.Web.Models;
public class ProductViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Price")]
    [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }

    [Required]
    [Display(Name = "Stock Quantity")]
    [Range(0, 10000, ErrorMessage = "Stock quantity must be between 0 and 10000")]
    public int StockQuantity { get; set; }

    public string? Description { get; set; }

    public string? ImageName { get; set; }

    public string? ImageUrl { get; set; }

    public bool HasImage => !string.IsNullOrEmpty(ImageUrl) || !string.IsNullOrEmpty(ImageName);

    [Display(Name = "Product Image")]
    public IFormFile? ImageFile { get; set; }

    // --- Review properties ---
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? ExternalReviewApiProductId { get; set; }
    public List<SG01G02_MVC.Application.DTOs.ReviewDto> Reviews { get; set; } = new();
}