namespace SG01G02_MVC.Application.DTOs
{
    public class ReviewDto
    {
        public string? Id { get; set; }
        public string? ProductId { get; set; }
        public string? Author { get; set; }
        public string? Content { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}