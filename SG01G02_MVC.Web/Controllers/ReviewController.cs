using Microsoft.AspNetCore.Mvc;
using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Application.DTOs;
using SG01G02_MVC.Web.Models;

namespace SG01G02_MVC.Web.Controllers;

public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductReviews(int productId)
    {
        try
        {
            var reviews = await _reviewService.GetReviewsForProduct(productId);
            return Json(new { success = true, reviews });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid product ID: {ProductId}", productId);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews for product {ProductId}", productId);
            return StatusCode(500, new { success = false, message = "An error occurred while fetching reviews." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(ReviewSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ReviewError"] = "Please fill in all required fields correctly.";
            return RedirectToAction("Details", "Catalogue", new { id = model.ProductId });
        }

        _logger.LogInformation("Submitting review for ProductId: {ProductId}, Name: {Name}, Rating: {Rating}", model.ProductId, model.CustomerName, model.Rating);

        var reviewDto = new ReviewDto
        {
            ProductId = model.ProductId,
            CustomerName = model.CustomerName,
            Rating = model.Rating,
            Content = model.Content,
            CreatedAt = DateTime.UtcNow
        };

        var success = await _reviewService.SubmitReviewAsync(reviewDto);
        if (success)
        {
            TempData["ReviewSuccess"] = "Thank you for your review!";
        }
        else
        {
            TempData["ReviewError"] = "There was a problem submitting your review. Please try again. (API call failed)";
        }
        return RedirectToAction("Details", "Catalogue", new { id = model.ProductId });
    }
}