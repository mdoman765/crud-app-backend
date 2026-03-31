using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crud_app_backend.Models
{
    /// <summary>
    /// Stores every incoming WhatsApp message from 360dialog via n8n.
    /// One row per message — type = 'text' | 'audio' | 'image'.
    /// </summary>
    public class WhatsAppMessage
    {
        [Key]
        [Column("Id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>360dialog / WhatsApp unique message ID — used to prevent duplicates.</summary>
        [Required]
        [MaxLength(255)]
        [Column("MessageId")]
        public string MessageId { get; set; } = default!;

        /// <summary>Sender phone number e.g. 8801XXXXXXXXX</summary>
        [Required]
        [MaxLength(30)]
        [Column("FromNumber")]
        public string FromNumber { get; set; } = default!;

        /// <summary>Display name from 360dialog contacts[] array.</summary>
        [MaxLength(255)]
        [Column("SenderName")]
        public string? SenderName { get; set; }

        /// <summary>text | audio | image | unsupported</summary>
        [Required]
        [MaxLength(20)]
        [Column("MessageType")]
        public string MessageType { get; set; } = default!;

        // ── Text ─────────────────────────────────────────────────────────────
        /// <summary>Filled only when MessageType = 'text'. NULL otherwise.</summary>
        [Column("TextBody", TypeName = "nvarchar(max)")]
        public string? TextBody { get; set; }

        // ── Media (audio + image shared) ──────────────────────────────────────
        /// <summary>Raw 360dialog media ID — kept for reference / re-download.</summary>
        [MaxLength(255)]
        [Column("MediaId")]
        public string? MediaId { get; set; }

        /// <summary>Public URL after file is saved to wwwroot/wa-media/.</summary>
        [MaxLength(2048)]
        [Column("FileUrl")]
        public string? FileUrl { get; set; }

        [MaxLength(100)]
        [Column("MimeType")]
        public string? MimeType { get; set; }

        [Column("FileSizeBytes")]
        public long? FileSizeBytes { get; set; }

        // ── Image only ────────────────────────────────────────────────────────
        [MaxLength(1000)]
        [Column("Caption")]
        public string? Caption { get; set; }

        // ── Audio only ────────────────────────────────────────────────────────
        [Column("DurationSeconds")]
        public int? DurationSeconds { get; set; }

        // ── Processing state ──────────────────────────────────────────────────
        /// <summary>received | processing | processed | failed</summary>
        [Required]
        [MaxLength(30)]
        [Column("Status")]
        public string Status { get; set; } = "received";

        [MaxLength(1000)]
        [Column("ErrorMessage")]
        public string? ErrorMessage { get; set; }

        // ── Timestamps ────────────────────────────────────────────────────────
        /// <summary>Unix epoch timestamp sent by 360dialog.</summary>
        [Column("WaTimestamp")]
        public long WaTimestamp { get; set; }

        [Column("ReceivedAt")]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }
    }
}
