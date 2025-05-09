namespace SG01G02_MVC.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        // TODO: Property need a proper implementation later on
        public int StockQuantity { get; set; }
        
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

        public string? ImageUrl { get; set; }
    }
}