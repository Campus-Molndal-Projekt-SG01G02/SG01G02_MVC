using System.ComponentModel.DataAnnotations;

namespace SG01G02_MVC.Web.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }
        
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        // --- Review properties ---
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<SG01G02_MVC.Application.DTOs.ReviewDto> Reviews { get; set; } = new();
    }
}