namespace SG01G02_MVC.Application.DTOs
{
    public class ReviewDto
    {
        public string? Id { get; set; }
        public int ProductId { get; set; }
        public string? CustomerName { get; set; }
        public string? Content { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
    }
}