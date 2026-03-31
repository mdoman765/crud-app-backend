using crud_app_backend.Models;

namespace crud_app_backend.Repositories
{
    public interface IWhatsAppMessageRepository
    {
        /// <summary>
        /// Returns true if a message with this 360dialog MessageId already exists.
        /// Used to guard against duplicate deliveries from 360dialog.
        /// </summary>
        Task<bool> ExistsAsync(string messageId, CancellationToken ct = default);

        /// <summary>
        /// Inserts a new message row and returns the saved entity.
        /// </summary>
        Task<WhatsAppMessage> InsertAsync(WhatsAppMessage message, CancellationToken ct = default);

        /// <summary>
        /// Updates Status, FileUrl, ErrorMessage and ProcessedAt for a row.
        /// Called after a media file has been uploaded to disk.
        /// </summary>
        Task UpdateStatusAsync(
            Guid id,
            string status,
            string? fileUrl,
            string? errorMessage,
            CancellationToken ct = default);

        /// <summary>
        /// Returns a single message by its 360dialog MessageId, or null.
        /// </summary>
        Task<WhatsAppMessage?> GetByMessageIdAsync(string messageId, CancellationToken ct = default);

        /// <summary>
        /// Returns the last <paramref name="limit"/> messages for a phone number, newest-first.
        /// </summary>
        Task<List<WhatsAppMessage>> GetByPhoneAsync(
            string phone, int limit = 20, CancellationToken ct = default);

        /// <summary>
        /// Returns the most recent <paramref name="limit"/> messages across all senders.
        /// </summary>
        Task<List<WhatsAppMessage>> GetRecentAsync(int limit = 20, CancellationToken ct = default);
    }
}
