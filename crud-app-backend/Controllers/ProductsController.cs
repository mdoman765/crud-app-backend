using crud_app_backend.Services;
using CrudApp.API.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace crud_app_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly IWebHostEnvironment _env;

        public ProductsController(IProductService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        // GET api/products
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
           
            [FromQuery] bool? isActive)
        {
            var products = await _service.GetAllAsync(search,  isActive);
            return Ok(products);
        }

        // GET api/products/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _service.GetCategoriesAsync();
            return Ok(categories);
        }

        // GET api/products/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            return product == null ? NotFound() : Ok(product);
        }

        // POST api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT api/products/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // ── Delete old image file if URL changed ──────────────────
            var existing = await _service.GetByIdAsync(id);
            if (existing != null &&
                !string.IsNullOrEmpty(existing.ImageUrl) &&
                existing.ImageUrl != dto.ImageUrl)
            {
                DeleteImageFile(existing.ImageUrl);
            }

            var updated = await _service.UpdateAsync(id, dto);
            return updated == null ? NotFound() : Ok(updated);
        }

        // DELETE api/products/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // ── Delete image file when product is deleted ─────────────
            var existing = await _service.GetByIdAsync(id);
            if (existing != null && !string.IsNullOrEmpty(existing.ImageUrl))
                DeleteImageFile(existing.ImageUrl);

            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        // ── Helper: extract filename from URL and delete from disk ───
        private void DeleteImageFile(string imageUrl)
        {
            try
            {
                var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                var filePath = Path.Combine(_env.WebRootPath, "images", fileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch
            {
                // Log but don't fail the request if file cleanup fails
            }
        }
    }

}
