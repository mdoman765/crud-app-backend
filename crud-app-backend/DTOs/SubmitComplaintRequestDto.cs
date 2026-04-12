using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace crud_app_backend.DTOs
{
    /// <summary>
    /// Sent by n8n when a staff member confirms Y on the complaint/agent screen.
    /// </summary>
    public class SubmitComplaintRequestDto
    {
        // ── Who is submitting ─────────────────────────────────────────────────

        [Required]
        [JsonPropertyName("whatsapp_phone")]
        public string WhatsappPhone { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("staff_id")]
        public string StaffId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("official_phone")]
        public string? OfficialPhone { get; set; }

        [JsonPropertyName("designation")]
        public string? Designation { get; set; }

        [JsonPropertyName("dept")]
        public string? Dept { get; set; }

        [JsonPropertyName("groupname")]
        public string? GroupName { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("locationname")]
        public string? LocationName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        // ── Complaint content ─────────────────────────────────────────────────

        /// <summary>
        /// All text messages joined with newline. Supports both Bangla and English.
        /// For agent connect this is: "User wants to connect with a support agent"
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// "complaint" or "agent_connect".
        /// Defaults to "complaint" if not provided.
        /// </summary>
        [JsonPropertyName("complaint_type")]
        public string ComplaintType { get; set; } = "complaint";

        /// <summary>
        /// 360dialog messageIds of uploaded voice notes.
        /// Empty list if user sent no voice notes.
        /// </summary>
        [JsonPropertyName("voice_message_ids")]
        public List<string> VoiceMessageIds { get; set; } = new();

        /// <summary>
        /// 360dialog messageIds of uploaded images.
        /// Empty list if user sent no images.
        /// </summary>
        [JsonPropertyName("image_message_ids")]
        public List<string> ImageMessageIds { get; set; } = new();
    }
}
