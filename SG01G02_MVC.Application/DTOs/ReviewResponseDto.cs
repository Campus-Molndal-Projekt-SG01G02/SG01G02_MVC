namespace SG01G02_MVC.Application.DTOs
{
    public class ReviewResponseDto
    {
        public List<ReviewDto> Reviews { get; set; }
        public ReviewStatsDto Stats { get; set; }
    }

    public class ReviewStatsDto
    {
        public int ProductId { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
