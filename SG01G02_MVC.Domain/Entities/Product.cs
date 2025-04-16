namespace SG01G02_MVC.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        // TODO: Property need a proper implementation later on
        public bool InStock { get; set; } = true;

        public string ImageUrl { get; set; } = string.Empty; // URL to product image
    }
}