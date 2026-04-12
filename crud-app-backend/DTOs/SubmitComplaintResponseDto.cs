using System.Text.Json.Serialization;

namespace crud_app_backend.DTOs
{
    public class SubmitComplaintResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// CRM ticket ID on success (e.g. "13").
        /// Null on failure.
        /// </summary>
        [JsonPropertyName("complaint_id")]
        public string? ComplaintId { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Populated when success=false.
        /// n8n reads this and shows it directly to the user.
        /// </summary>
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>Represents a stored WhatsApp media file loaded for CRM forwarding.</summary>
    public class WhatsAppMediaFile
    {
        public string MessageId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string? Caption { get; set; }
    }
}
