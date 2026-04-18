using crud_app_backend.DTOs;
using crud_app_backend.Models;

namespace crud_app_backend.Repositories
{
    public interface IWhatsAppSessionRepository
    {
        /// <summary>
        /// Returns the session for a phone number, or null if none exists.
        /// </summary>
        Task<WhatsAppSession?> GetByPhoneAsync(string phone, CancellationToken ct = default);

        /// <summary>
        /// Atomically insert-or-update the session and write one history row.
        /// When <paramref name="preserveComplaintMedia"/> is true the repository
        /// keeps the complaint_images and complaint_voices arrays that are
        /// already in the database, ignoring whatever is in the incoming TempData
        /// for those two keys. Use this flag on every regular "state-machine write"
        /// so that concurrent media appends are never clobbered.
        /// </summary>
        Task UpsertAsync(
            WhatsAppSession session,
            string? rawMessage,
            bool preserveComplaintMedia = false,
            CancellationToken ct = default);

        /// <summary>
        /// Atomically appends one media messageId to complaint_images or
        /// complaint_voices inside TempData.
        ///
        /// Uses an UPDLOCK row-level hint so concurrent calls for the same
        /// phone number are serialized at the database level — no two executions
        /// can read-modify-write the JSON array simultaneously.
        ///
        /// Returns the FULL updated arrays so n8n can build its updatedSession
        /// from fresh data instead of a stale in-memory copy.
        /// </summary>
        Task<AppendMediaResponseDto> AppendMediaAsync(
            string phone,
            string mediaType,   // "image" | "voice"
            string messageId,
            CancellationToken ct = default);

        /// <summary>
        /// Hard-delete a session (and its history via CASCADE).
        /// </summary>
        Task<bool> DeleteAsync(string phone, CancellationToken ct = default);

        /// <summary>
        /// Returns the last <paramref name="limit"/> history rows for a phone,
        /// newest first.
        /// </summary>
        Task<List<WhatsAppSessionHistory>> GetHistoryAsync(
            string phone, int limit = 20, CancellationToken ct = default);
    }
}
