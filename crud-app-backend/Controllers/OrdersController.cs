using crud_app_backend.DTOs;
using crud_app_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace crud_app_backend.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service)
        {
            _service = service;
        }

        [HttpPost("user/{userId:int}/subcategory/{subCategoryId:int}")]
        public async Task<IActionResult> CreateOrder(
           
            [FromBody] CreateOrderDto dto)
        {
            

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _service.CreateOrderByUserAndSubCategoryAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _service.GetOrderByIdAsync(id);
            return order == null ? NotFound() : Ok(order);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var orders = await _service.GetOrdersByUserIdAsync(userId);
            return Ok(orders);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var updated = await _service.UpdateOrderStatusAsync(id, status);
            return updated ? NoContent() : NotFound();
        }

        //// Add Delete if needed
        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var deleted = await _service.DeleteAsync(id); // Add DeleteAsync to IOrderService and impl if needed
        //    return deleted ? NoContent() : NotFound();
        //}
    }
}
