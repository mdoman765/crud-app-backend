using System.ComponentModel.DataAnnotations;

namespace crud_app_backend.DTOs
{
    public class UpsertSessionRequestDto
    {
        [Required]
        public string Phone { get; set; } = default!;

        [Required]
        public string CurrentStep { get; set; } = default!;

        /// <summary>
        /// Full JSON string — whatever the workflow needs to persist.
        /// e.g. {"menu_map":{"1":"category"},"selected_category":3}
        /// </summary>
        public string TempData { get; set; } = "{}";

        public string PreviousStep { get; set; } = "INIT";
        public bool PendingReport { get; set; } = false;
        public bool PendingShopReg { get; set; } = false;

        /// <summary>Optional: raw message text, stored in audit log.</summary>
        public string? RawMessage { get; set; }

        /// <summary>
        /// When true, the repository will IGNORE complaint_images and complaint_voices
        /// from the incoming TempData and instead preserve whatever arrays are
        /// currently in the database row.
        ///
        /// Set this to true in every n8n "Prepare Send → POST Session to DB" call
        /// so that concurrent image/voice appends are never overwritten by a
        /// slightly-delayed full-session write.
        ///
        /// Only set to false (the default) when you intentionally want to CLEAR
        /// the media arrays — e.g. when starting a brand-new complaint.
        /// </summary>
        public bool PreserveComplaintMedia { get; set; } = false;
    }
}
