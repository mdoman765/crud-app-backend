using System.ComponentModel.DataAnnotations;

namespace crud_app_backend.DTOs
{
    /// <summary>
    /// Body for POST /api/whatsapp/session/append-media
    /// Atomically appends one media messageId to complaint_images or complaint_voices
    /// inside TempData without overwriting the rest of the session.
    /// </summary>
    public class AppendMediaRequestDto
    {
        [Required]
        public string Phone { get; set; } = default!;

        /// <summary>"image" or "voice"</summary>
        [Required]
        [RegularExpression("^(image|voice)$",
            ErrorMessage = "mediaType must be 'image' or 'voice'")]
        public string MediaType { get; set; } = default!;

        [Required]
        public string MessageId { get; set; } = default!;
    }

    /// <summary>
    /// Returned by append-media so n8n can build updatedSession
    /// from the REAL DB arrays instead of stale in-memory copies.
    /// </summary>
    public class AppendMediaResponseDto
    {
        public string Phone { get; set; } = default!;

        /// <summary>All complaint image messageIds now stored in DB (after append).</summary>
        public List<string> ComplaintImages { get; set; } = new();

        /// <summary>All complaint voice messageIds now stored in DB (after append).</summary>
        public List<string> ComplaintVoices { get; set; } = new();
    }
}
