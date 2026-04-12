using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crud_app_backend.Models
{
    /// <summary>
    /// One row per complaint or agent-connect request submitted via WhatsApp.
    /// Linked to WhatsAppComplaintMedia for voice notes and images.
    /// </summary>
    public class WhatsAppComplaint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Human-readable ticket number e.g. PR00001.
        /// NULL on first insert, set to "PR{Id:D5}" immediately after.
        /// Nullable so the unique index allows multiple NULLs without conflict.
        /// </summary>
        [MaxLength(20)]
        public string? ComplaintNumber { get; set; } = null;

        // ── Who submitted ─────────────────────────────────────────────────────
        [Required][MaxLength(30)] public string WhatsappPhone { get; set; } = default!;
        [Required][MaxLength(50)] public string StaffId { get; set; } = default!;
        [MaxLength(255)] public string? StaffName { get; set; }
        [MaxLength(30)] public string? OfficialPhone { get; set; }
        [MaxLength(100)] public string? Designation { get; set; }
        [MaxLength(100)] public string? Dept { get; set; }
        [MaxLength(100)] public string? GroupName { get; set; }
        [MaxLength(100)] public string? Company { get; set; }
        [MaxLength(200)] public string? LocationName { get; set; }
        [MaxLength(255)] public string? Email { get; set; }

        // ── Complaint content ─────────────────────────────────────────────────

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        /// <summary>
        /// "complaint" | "agent_connect"
        /// </summary>
        [MaxLength(30)]
        public string ComplaintType { get; set; } = "complaint";

        /// <summary>Ticket ID returned by CRM e.g. "13". Null until CRM call succeeds.</summary>
        [MaxLength(50)]
        public string? CrmTicketId { get; set; } = null;

        // ── Status ────────────────────────────────────────────────────────────
        /// <summary>open | in_progress | resolved | closed</summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "open";

        // ── Timestamps ────────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ────────────────────────────────────────────────────────
        public ICollection<WhatsAppComplaintMedia> MediaItems { get; set; } = new List<WhatsAppComplaintMedia>();
    }
}
