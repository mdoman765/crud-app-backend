using crud_app_backend.DTOs;
using crud_app_backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace crud_app_backend.Controllers
{
    [ApiController]
    [Route("api/whatsapp/complaints")]
    [Produces("application/json")]
    public class WhatsAppComplaintController : ControllerBase
    {
        private readonly IWhatsAppComplaintService _service;
        private readonly AppDbContext _db;
        private readonly ILogger<WhatsAppComplaintController> _logger;

        public WhatsAppComplaintController(
            IWhatsAppComplaintService service,
            AppDbContext db,
            ILogger<WhatsAppComplaintController> logger)
        {
            _service = service;
            _db = db;
            _logger = logger;
        }

        // POST /api/whatsapp/complaints/submit
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            [FromBody] SubmitComplaintRequestDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.SubmitAsync(dto, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Complaint] Submit crashed — staff={S} phone={P}",
                    dto.StaffId, dto.WhatsappPhone);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET /api/whatsapp/complaints
        // ?phone=8801...   filter by WhatsApp phone
        // ?staff_id=359778 filter by staff ID
        // ?status=open     filter by status (open / in_progress / resolved / closed)
        // ?limit=50        max rows (default 50, max 200)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? phone,
            [FromQuery] string? staff_id,
            [FromQuery] string? status,
            [FromQuery] int limit = 50,
            CancellationToken ct = default)
        {
            if (limit is < 1 or > 200) limit = 50;

            var query = _db.WhatsAppComplaints
                .Include(c => c.MediaItems)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(phone))
                query = query.Where(c => c.WhatsappPhone == phone.Trim());

            if (!string.IsNullOrWhiteSpace(staff_id))
                query = query.Where(c => c.StaffId == staff_id.Trim());

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status.Trim().ToLower());

            var rows = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .Select(c => new
                {
                    c.Id,
                    c.ComplaintNumber,
                    c.WhatsappPhone,
                    c.StaffId,
                    c.StaffName,
                    c.OfficialPhone,
                    c.Designation,
                    c.Dept,
                    c.GroupName,
                    c.Company,
                    c.LocationName,
                    c.Email,
                    c.Description,
                    c.Status,
                    c.CreatedAt,
                    c.UpdatedAt,
                    MediaCount = c.MediaItems.Count,
                    VoiceCount = c.MediaItems.Count(m => m.MediaType == "voice"),
                    ImageCount = c.MediaItems.Count(m => m.MediaType == "image"),
                })
                .ToListAsync(ct);

            return Ok(new { total = rows.Count, data = rows });
        }

        // GET /api/whatsapp/complaints/{id}
        // id = numeric (7)  OR  complaint number (PR00007)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(string id, CancellationToken ct)
        {
            var query = _db.WhatsAppComplaints
                .Include(c => c.MediaItems)
                .AsNoTracking();

            var complaint = int.TryParse(id, out var numId)
                ? await query.FirstOrDefaultAsync(c => c.Id == numId, ct)
                : await query.FirstOrDefaultAsync(c => c.ComplaintNumber == id.ToUpper(), ct);

            if (complaint is null)
                return NotFound(new { message = $"Complaint '{id}' not found." });

            return Ok(new
            {
                complaint.Id,
                complaint.ComplaintNumber,
                complaint.WhatsappPhone,
                complaint.StaffId,
                complaint.StaffName,
                complaint.OfficialPhone,
                complaint.Designation,
                complaint.Dept,
                complaint.GroupName,
                complaint.Company,
                complaint.LocationName,
                complaint.Email,
                complaint.Description,
                complaint.Status,
                complaint.CreatedAt,
                complaint.UpdatedAt,
                Media = complaint.MediaItems.Select(m => new
                {
                    m.Id,
                    m.MediaType,
                    m.MessageId,
                    m.FileUrl,
                    m.MimeType,
                    m.Caption,
                    m.CreatedAt,
                })
            });
        }
    }
}
