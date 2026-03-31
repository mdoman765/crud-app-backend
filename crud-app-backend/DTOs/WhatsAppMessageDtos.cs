using System.ComponentModel.DataAnnotations;

namespace crud_app_backend.DTOs
{
    public class IncomingTextMessageDto
    {
        [Required]
        public string MessageId { get; set; } = default!;

        [Required]
        public string From { get; set; } = default!;

        public string? SenderName { get; set; }

        [Required]
        public string Text { get; set; } = default!;

        public long Timestamp { get; set; }
    }

    /// <summary>POST /api/whatsapp/messages/voice  — multipart/form-data</summary>
    public class IncomingVoiceMessageDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string MessageId { get; set; } = default!;

        [Required]
        public string From { get; set; } = default!;

        public string? SenderName { get; set; }
        public string? MimeType { get; set; }
        public long Timestamp { get; set; }
    }

    /// <summary>POST /api/whatsapp/messages/image  — multipart/form-data</summary>
    public class IncomingImageMessageDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string MessageId { get; set; } = default!;

        [Required]
        public string From { get; set; } = default!;

        public string? SenderName { get; set; }
        public string? Caption { get; set; }
        public string? MimeType { get; set; }
        public long Timestamp { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Response DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returned immediately after a message is received/saved.</summary>
    public class WhatsAppMessageReceivedDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = "received";
        public string MessageId { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string? FileUrl { get; set; }
    }

    /// <summary>Returned in list queries (GET).</summary>
    public class WhatsAppMessageListDto
    {
        public Guid Id { get; set; }
        public string MessageId { get; set; } = default!;
        public string FromNumber { get; set; } = default!;
        public string? SenderName { get; set; }
        public string MessageType { get; set; } = default!;
        public string? Preview { get; set; }
        public string? FileUrl { get; set; }
        public string Status { get; set; } = default!;
        public DateTime ReceivedAt { get; set; }
    }
}
