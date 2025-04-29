namespace SG01G02_MVC.Application.DTOs
{
    public class ReviewDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}