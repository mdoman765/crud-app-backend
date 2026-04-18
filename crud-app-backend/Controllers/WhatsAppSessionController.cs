using crud_app_backend.DTOs;
using crud_app_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace crud_app_backend.Controllers
{
    [ApiController]
    [Route("api/whatsapp")]
    [Produces("application/json")]
    public class WhatsAppSessionController : ControllerBase
    {
        private readonly IWhatsAppSessionService _service;
        private readonly ILogger<WhatsAppSessionController> _logger;

        public WhatsAppSessionController(
            IWhatsAppSessionService service,
            ILogger<WhatsAppSessionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /api/whatsapp/session?phone=8801XXXXXXXXX
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("session")]
        [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSession(
            [FromQuery] string phone,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { error = "phone query parameter is required" });

            try
            {
                var result = await _service.GetSessionAsync(phone.Trim(), ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Controller] GetSession failed — phone={Phone}", phone);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to load session: " + ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /api/whatsapp/session
        // Full session upsert called by "Prepare Send → POST Session to DB".
        // Pass PreserveComplaintMedia: true in the body to prevent this write
        // from clobbering atomically-appended media arrays.
        // ─────────────────────────────────────────────────────────────────
        [HttpPost("session")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertSession(
            [FromBody] UpsertSessionRequestDto req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.UpsertSessionAsync(req, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Controller] UpsertSession failed — phone={Phone} step={Step}",
                    req.Phone, req.CurrentStep);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to save session: " + ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /api/whatsapp/session/append-media          ← NEW ENDPOINT
        //
        // Atomically appends one media messageId to complaint_images or
        // complaint_voices inside TempData, using a DB-level UPDLOCK row hint
        // to serialize concurrent n8n executions for the same phone number.
        //
        // Body (JSON):
        // {
        //   "phone":     "8801XXXXXXXXX",
        //   "mediaType": "image",           // or "voice"
        //   "messageId": "wamid.HBg..."
        // }
        //
        // Response:
        // {
        //   "phone":           "8801XXXXXXXXX",
        //   "complaintImages": ["wamid.A", "wamid.B", "wamid.C"],
        //   "complaintVoices": []
        // }
        //
        // n8n should call this endpoint right after "POST Image/Voice to Backend"
        // and use the returned arrays to build updatedSession, instead of doing
        // the local array-append in the old "Store Complaint Image/Voice" Code nodes.
        // ─────────────────────────────────────────────────────────────────
        [HttpPost("session/append-media")]
        [ProducesResponseType(typeof(AppendMediaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AppendMedia(
            [FromBody] AppendMediaRequestDto req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.AppendMediaAsync(req, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex,
                    "[WA-Controller] AppendMedia — session not found phone={Phone}",
                    req.Phone);

                return NotFound(ApiResponseDto<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Controller] AppendMedia failed — phone={Phone} type={Type}",
                    req.Phone, req.MediaType);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to append media: " + ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE /api/whatsapp/session?phone=...
        // ─────────────────────────────────────────────────────────────────
        [HttpDelete("session")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteSession(
            [FromQuery] string phone,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { error = "phone query parameter is required" });

            try
            {
                var result = await _service.DeleteSessionAsync(phone.Trim(), ct);

                if (!result.Success)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Controller] DeleteSession failed — phone={Phone}", phone);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to delete session: " + ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /api/whatsapp/session/history?phone=...&limit=20
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("session/history")]
        [ProducesResponseType(typeof(List<Models.WhatsAppSessionHistory>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string phone,
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { error = "phone query parameter is required" });

            if (limit is < 1 or > 200)
                limit = 20;

            try
            {
                var rows = await _service.GetHistoryAsync(phone.Trim(), limit, ct);
                return Ok(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Controller] GetHistory failed — phone={Phone}", phone);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to load history: " + ex.Message));
            }
        }
    }
}
