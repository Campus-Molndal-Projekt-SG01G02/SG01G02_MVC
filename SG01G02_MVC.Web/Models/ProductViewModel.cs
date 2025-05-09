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
    }
}