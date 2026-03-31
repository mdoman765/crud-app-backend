using crud_app_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace crud_app_backend.Repositories
{
    public class WhatsAppMessageRepository : IWhatsAppMessageRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<WhatsAppMessageRepository> _logger;

        public WhatsAppMessageRepository(
            AppDbContext db,
            ILogger<WhatsAppMessageRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ── EXISTS ────────────────────────────────────────────────────────────

        public async Task<bool> ExistsAsync(string messageId, CancellationToken ct = default)
        {
            return await _db.WhatsAppMessages
                .AsNoTracking()
                .AnyAsync(m => m.MessageId == messageId, ct);
        }

        // ── INSERT ────────────────────────────────────────────────────────────

        public async Task<WhatsAppMessage> InsertAsync(
            WhatsAppMessage message, CancellationToken ct = default)
        {
            await _db.WhatsAppMessages.AddAsync(message, ct);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[WA-Msg-Repo] INSERT id={Id} type={Type} from={From}",
                message.Id, message.MessageType, message.FromNumber);

            return message;
        }

        // ── UPDATE STATUS ─────────────────────────────────────────────────────
        // Called once the media file has been written to wwwroot/wa-media/

        public async Task UpdateStatusAsync(
            Guid id,
            string status,
            string? fileUrl,
            string? errorMessage,
            CancellationToken ct = default)
        {
            var row = await _db.WhatsAppMessages
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (row is null)
            {
                _logger.LogWarning("[WA-Msg-Repo] UpdateStatus — row not found id={Id}", id);
                return;
            }

            row.Status = status;
            row.ProcessedAt = DateTime.UtcNow;
            row.ErrorMessage = errorMessage;

            if (fileUrl is not null)
                row.FileUrl = fileUrl;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[WA-Msg-Repo] UPDATE id={Id} status={Status} fileUrl={Url}",
                id, status, fileUrl);
        }

        // ── QUERIES ───────────────────────────────────────────────────────────

        public async Task<WhatsAppMessage?> GetByMessageIdAsync(
            string messageId, CancellationToken ct = default)
        {
            return await _db.WhatsAppMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MessageId == messageId, ct);
        }

        public async Task<List<WhatsAppMessage>> GetByPhoneAsync(
            string phone, int limit = 20, CancellationToken ct = default)
        {
            return await _db.WhatsAppMessages
                .AsNoTracking()
                .Where(m => m.FromNumber == phone)
                .OrderByDescending(m => m.ReceivedAt)
                .Take(Math.Clamp(limit, 1, 200))
                .ToListAsync(ct);
        }

        public async Task<List<WhatsAppMessage>> GetRecentAsync(
            int limit = 20, CancellationToken ct = default)
        {
            return await _db.WhatsAppMessages
                .AsNoTracking()
                .OrderByDescending(m => m.ReceivedAt)
                .Take(Math.Clamp(limit, 1, 200))
                .ToListAsync(ct);
        }
    }
}
