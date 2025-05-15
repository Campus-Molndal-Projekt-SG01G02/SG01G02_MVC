namespace SG01G02_MVC.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageName { get; set; }
        public string? ImageUrl { get; set; }
        // public bool HasImage => !string.IsNullOrEmpty(ImageName);
        public bool HasImage => !string.IsNullOrEmpty(ImageName) || !string.IsNullOrEmpty(ImageUrl);
    }
}