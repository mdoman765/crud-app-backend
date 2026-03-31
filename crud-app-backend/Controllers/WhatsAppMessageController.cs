using crud_app_backend.DTOs;
using crud_app_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace crud_app_backend.Controllers
{
    /// <summary>
    /// Receives incoming WhatsApp messages (text / voice / image) forwarded
    /// by n8n from 360dialog.  Lives at /api/whatsapp/messages — separate
    /// from the existing /api/whatsapp/session routes.
    /// </summary>
    [ApiController]
    [Route("api/whatsapp/messages")]
    [Produces("application/json")]
    public class WhatsAppMessageController : ControllerBase
    {
        private readonly IWhatsAppMessageService _service;
        private readonly ILogger<WhatsAppMessageController> _logger;

        public WhatsAppMessageController(
            IWhatsAppMessageService service,
            ILogger<WhatsAppMessageController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/whatsapp/messages/text
        // Content-Type: application/json
        // Body: { messageId, from, senderName?, text, timestamp }
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("text")]
        [ProducesResponseType(typeof(WhatsAppMessageReceivedDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReceiveText(
            [FromBody] IncomingTextMessageDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.HandleTextAsync(dto, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Messages] ReceiveText failed — messageId={Id} from={From}",
                    dto.MessageId, dto.From);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to save text message: " + ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/whatsapp/messages/voice
        // Content-Type: multipart/form-data
        // Fields: file (binary), messageId, from, senderName?, mimeType?, timestamp
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("voice")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)]   // 25 MB — WhatsApp audio cap
        [ProducesResponseType(typeof(WhatsAppMessageReceivedDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReceiveVoice(
            [FromForm] IncomingVoiceMessageDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.File is null || dto.File.Length == 0)
                return BadRequest(new { error = "file is required and must not be empty" });

            try
            {
                var result = await _service.HandleVoiceAsync(dto, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Messages] ReceiveVoice failed — messageId={Id} from={From}",
                    dto.MessageId, dto.From);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to save voice message: " + ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/whatsapp/messages/image
        // Content-Type: multipart/form-data
        // Fields: file (binary), messageId, from, senderName?, caption?, mimeType?, timestamp
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(16 * 1024 * 1024)]   // 16 MB — WhatsApp image cap
        [ProducesResponseType(typeof(WhatsAppMessageReceivedDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReceiveImage(
            [FromForm] IncomingImageMessageDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.File is null || dto.File.Length == 0)
                return BadRequest(new { error = "file is required and must not be empty" });

            try
            {
                var result = await _service.HandleImageAsync(dto, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Messages] ReceiveImage failed — messageId={Id} from={From}",
                    dto.MessageId, dto.From);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to save image message: " + ex.Message));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/whatsapp/messages?phone=8801XXXXXXXXX&limit=20
        // Omit phone to get the most recent messages across all senders.
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        [ProducesResponseType(typeof(List<WhatsAppMessageListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessages(
            [FromQuery] string? phone,
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            if (limit is < 1 or > 200) limit = 20;

            try
            {
                var rows = string.IsNullOrWhiteSpace(phone)
                    ? await _service.GetRecentAsync(limit, ct)
                    : await _service.GetByPhoneAsync(phone, limit, ct);

                return Ok(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WA-Messages] GetMessages failed — phone={Phone}", phone);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.Fail("Failed to load messages: " + ex.Message));
            }
        }
    }
}
