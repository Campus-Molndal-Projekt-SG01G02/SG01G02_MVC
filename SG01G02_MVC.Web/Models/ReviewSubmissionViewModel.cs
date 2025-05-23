using System.ComponentModel.DataAnnotations;

namespace SG01G02_MVC.Web.Models;

public class ReviewSubmissionViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Display(Name = "Your Name")]
    [StringLength(50)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [Display(Name = "Review")]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
} 