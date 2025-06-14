namespace SG01G02_MVC.Application.DTOs;
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageName { get; set; }
    public string? ImageUrl { get; set; }
    public bool HasImage => !string.IsNullOrEmpty(ImageUrl) || !string.IsNullOrEmpty(ImageName);
    public int? ExternalReviewApiProductId { get; set; }
}