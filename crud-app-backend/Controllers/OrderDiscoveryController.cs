using crud_app_backend.DTOs;
using crud_app_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace crud_app_backend.Controllers
{
    [ApiController]
    [Route("api/order-discovery")]
    public class OrderDiscoveryController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrderDiscoveryController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _service.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("subcategories")]
        public async Task<IActionResult> GetSubCategories([FromQuery] int category)
        {
            

            var subCategories = await _service.GetSubCategoriesAsync(category);
            return Ok(subCategories);
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProductsByCategorySubCategory(
           
            [FromQuery] int subCategory)
        {
            

            var products = await _service.GetProductsByCategorySubCategoryAsync(subCategory);
            return Ok(products);
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var feedback = await _service.SubmitFeedbackAsync(dto);
            return Created($"/api/order-discovery/feedback/user/{dto.UserId}", feedback);
        }

        [HttpGet("feedback/user/{userId:int}")]
        public async Task<IActionResult> GetFeedbackByUserId(int userId)
        {
            var feedbacks = await _service.GetFeedbackByUserIdAsync(userId);
            return Ok(feedbacks);
        }
    }
}
