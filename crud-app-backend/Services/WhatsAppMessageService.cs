using crud_app_backend.DTOs;
using crud_app_backend.Models;
using crud_app_backend.Repositories;

namespace crud_app_backend.Services
{
    public class WhatsAppMessageService : IWhatsAppMessageService
    {
        private readonly IWhatsAppMessageRepository _repo;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<WhatsAppMessageService> _logger;

        public WhatsAppMessageService(
            IWhatsAppMessageRepository repo,
            IWebHostEnvironment env,
            IHttpContextAccessor http,
            ILogger<WhatsAppMessageService> logger)
        {
            _repo = repo;
            _env = env;
            _http = http;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEXT
        // ─────────────────────────────────────────────────────────────────────

        public async Task<WhatsAppMessageReceivedDto> HandleTextAsync(
            IncomingTextMessageDto dto, CancellationToken ct = default)
        {
            // Idempotency — 360dialog can deliver the same message more than once
            if (await _repo.ExistsAsync(dto.MessageId, ct))
            {
                _logger.LogWarning("[WA-Msg] Duplicate text — messageId={Id}", dto.MessageId);
                var dup = await _repo.GetByMessageIdAsync(dto.MessageId, ct);
                return MapToReceived(dup!);
            }

            var message = new WhatsAppMessage
            {
                MessageId = dto.MessageId,
                FromNumber = dto.From.Trim(),
                SenderName = dto.SenderName,
                MessageType = "text",
                TextBody = dto.Text,
                WaTimestamp = dto.Timestamp,
                Status = "processed",
                ProcessedAt = DateTime.UtcNow
            };

            await _repo.InsertAsync(message, ct);

            _logger.LogInformation(
                "[WA-Msg] Text saved — id={Id} from={From}",
                message.Id, message.FromNumber);

            return MapToReceived(message);
        }

        // ─────────────────────────────────────────────────────────────────────
        // VOICE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<WhatsAppMessageReceivedDto> HandleVoiceAsync(
            IncomingVoiceMessageDto dto, CancellationToken ct = default)
        {
            if (await _repo.ExistsAsync(dto.MessageId, ct))
            {
                _logger.LogWarning("[WA-Msg] Duplicate audio — messageId={Id}", dto.MessageId);
                var dup = await _repo.GetByMessageIdAsync(dto.MessageId, ct);
                return MapToReceived(dup!);
            }

            // 1. Insert row immediately (status = processing) so we have an Id
            var message = new WhatsAppMessage
            {
                MessageId = dto.MessageId,
                FromNumber = dto.From.Trim(),
                SenderName = dto.SenderName,
                MessageType = "audio",
                MimeType = dto.MimeType ?? "audio/ogg",
                WaTimestamp = dto.Timestamp,
                Status = "processing"
            };
            await _repo.InsertAsync(message, ct);

            // 2. Save the audio file to wwwroot/wa-media/audio/
            try
            {
                var ext = MimeToExt(dto.MimeType ?? "audio/ogg", ".ogg");
                var fileName = $"{dto.MessageId}{ext}";
                var fileUrl = await SaveFileAsync(dto.File, "audio", fileName, ct);

                message.FileUrl = fileUrl;
                message.FileSizeBytes = dto.File.Length;
                message.Status = "processed";
                message.ProcessedAt = DateTime.UtcNow;

                await _repo.UpdateStatusAsync(message.Id, "processed", fileUrl, null, ct);

                _logger.LogInformation(
                    "[WA-Msg] Audio saved — id={Id} url={Url}",
                    message.Id, fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WA-Msg] Audio file save failed — id={Id}", message.Id);
                await _repo.UpdateStatusAsync(message.Id, "failed", null, ex.Message, ct);
                message.Status = "failed";
            }

            return MapToReceived(message);
        }

        // ─────────────────────────────────────────────────────────────────────
        // IMAGE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<WhatsAppMessageReceivedDto> HandleImageAsync(
            IncomingImageMessageDto dto, CancellationToken ct = default)
        {
            if (await _repo.ExistsAsync(dto.MessageId, ct))
            {
                _logger.LogWarning("[WA-Msg] Duplicate image — messageId={Id}", dto.MessageId);
                var dup = await _repo.GetByMessageIdAsync(dto.MessageId, ct);
                return MapToReceived(dup!);
            }

            var message = new WhatsAppMessage
            {
                MessageId = dto.MessageId,
                FromNumber = dto.From.Trim(),
                SenderName = dto.SenderName,
                MessageType = "image",
                MimeType = dto.MimeType ?? "image/jpeg",
                Caption = dto.Caption,
                WaTimestamp = dto.Timestamp,
                Status = "processing"
            };
            await _repo.InsertAsync(message, ct);

            try
            {
                var ext = MimeToExt(dto.MimeType ?? "image/jpeg", ".jpg");
                var fileName = $"{dto.MessageId}{ext}";
                var fileUrl = await SaveFileAsync(dto.File, "images", fileName, ct);

                message.FileUrl = fileUrl;
                message.FileSizeBytes = dto.File.Length;
                message.Status = "processed";
                message.ProcessedAt = DateTime.UtcNow;

                await _repo.UpdateStatusAsync(message.Id, "processed", fileUrl, null, ct);

                _logger.LogInformation(
                    "[WA-Msg] Image saved — id={Id} url={Url}",
                    message.Id, fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WA-Msg] Image file save failed — id={Id}", message.Id);
                await _repo.UpdateStatusAsync(message.Id, "failed", null, ex.Message, ct);
                message.Status = "failed";
            }

            return MapToReceived(message);
        }

        // ─────────────────────────────────────────────────────────────────────
        // QUERIES
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<WhatsAppMessageListDto>> GetByPhoneAsync(
            string phone, int limit = 20, CancellationToken ct = default)
        {
            if (limit is < 1 or > 200) limit = 20;
            var rows = await _repo.GetByPhoneAsync(phone.Trim(), limit, ct);
            return rows.Select(MapToList).ToList();
        }

        public async Task<List<WhatsAppMessageListDto>> GetRecentAsync(
            int limit = 20, CancellationToken ct = default)
        {
            if (limit is < 1 or > 200) limit = 20;
            var rows = await _repo.GetRecentAsync(limit, ct);
            return rows.Select(MapToList).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Saves <paramref name="file"/> to wwwroot/wa-media/{subFolder}/
        /// and returns the absolute public URL — exactly how UploadController works.
        /// </summary>
        private async Task<string> SaveFileAsync(
            IFormFile file, string subFolder, string fileName, CancellationToken ct)
        {
            var folder = Path.Combine(_env.WebRootPath, "wa-media", subFolder);
            Directory.CreateDirectory(folder);   // safe — no-op if exists

            var filePath = Path.Combine(folder, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, ct);

            // Build absolute URL the same way UploadController does
            var req = _http.HttpContext!.Request;
            var baseUrl = $"{req.Scheme}://{req.Host}";
            return $"{baseUrl}/wa-media/{subFolder}/{fileName}";
        }

        private static WhatsAppMessageReceivedDto MapToReceived(WhatsAppMessage m) => new()
        {
            Id = m.Id,
            Status = m.Status,
            MessageId = m.MessageId,
            Type = m.MessageType,
            FileUrl = m.FileUrl
        };

        private static WhatsAppMessageListDto MapToList(WhatsAppMessage m) => new()
        {
            Id = m.Id,
            MessageId = m.MessageId,
            FromNumber = m.FromNumber,
            SenderName = m.SenderName,
            MessageType = m.MessageType,
            Preview = m.MessageType == "text"
                            ? (m.TextBody?.Length > 80
                                ? m.TextBody[..80] + "…"
                                : m.TextBody)
                            : m.FileUrl,
            FileUrl = m.FileUrl,
            Status = m.Status,
            ReceivedAt = m.ReceivedAt
        };

        private static string MimeToExt(string mime, string fallback) => mime switch
        {
            "audio/ogg" => ".ogg",
            "audio/mpeg" => ".mp3",
            "audio/wav" => ".wav",
            "audio/opus" => ".opus",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => fallback
        };
    }
}
