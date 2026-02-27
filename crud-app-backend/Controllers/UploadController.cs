using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace crud_app_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // ─────────────────────────────────────────────────────────────
        // POST api/upload
        // Accepts: multipart/form-data with field name "file"
        // Returns: { url: "http://localhost:5000/images/uuid.jpg" }
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // ── 1. Validate ───────────────────────────────────────────
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided." });

            var allowedTypes = new[]
            {
            "image/jpeg", "image/jpg", "image/png",
            "image/webp", "image/gif"
        };

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { error = "Only JPG, PNG, WEBP, GIF allowed." });

            const long maxBytes = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxBytes)
                return BadRequest(new { error = "File must be under 5 MB." });

            // ── 2. Build file path ────────────────────────────────────
            var folder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(folder);   // safe – does nothing if exists

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            // ── 3. Save file ──────────────────────────────────────────
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // ── 4. Return public URL ──────────────────────────────────
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var imageUrl = $"{baseUrl}/images/{fileName}";

            return Ok(new { url = imageUrl });
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE api/upload?fileName=uuid.jpg
        // Called automatically when a product image is replaced/removed
        // ─────────────────────────────────────────────────────────────
        [HttpDelete]
        public IActionResult Delete([FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest(new { error = "fileName is required." });

            // Security: strip any path separators (prevent directory traversal)
            fileName = Path.GetFileName(fileName);

            var filePath = Path.Combine(_env.WebRootPath, "images", fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { error = "File not found." });

            System.IO.File.Delete(filePath);

            return NoContent();
        }
    }
}
