using crud_app_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace crud_app_backend.Repositories
{
    public class WhatsAppComplaintRepository : IWhatsAppComplaintRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<WhatsAppComplaintRepository> _logger;

        public WhatsAppComplaintRepository(
            AppDbContext db,
            ILogger<WhatsAppComplaintRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ?? INSERT ????????????????????????????????????????????????????????????

        public async Task<WhatsAppComplaint> InsertAsync(
            WhatsAppComplaint complaint, CancellationToken ct = default)
        {
            await _db.WhatsAppComplaints.AddAsync(complaint, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "[Complaint-Repo] INSERT id={Id} staff={S} phone={P}",
                complaint.Id, complaint.StaffId, complaint.WhatsappPhone);
            return complaint;
        }

        // ?? UPDATE COMPLAINT NUMBER ???????????????????????????????????????????

        public async Task UpdateComplaintNumberAsync(
            int id, string number, CancellationToken ct = default)
        {
            var row = await _db.WhatsAppComplaints
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (row is null) return;
            row.ComplaintNumber = number;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // ?? INSERT MEDIA ??????????????????????????????????????????????????????

        public async Task InsertMediaAsync(
            WhatsAppComplaintMedia media, CancellationToken ct = default)
        {
            await _db.WhatsAppComplaintMediaItems.AddAsync(media, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "[Complaint-Repo] MEDIA complaintId={C} type={T} msgId={M}",
                media.ComplaintId, media.MediaType, media.MessageId);
        }

        // ?? QUERIES ???????????????????????????????????????????????????????????

        public async Task UpdateCrmTicketIdAsync(int id, string crmTicketId, CancellationToken ct = default)
        {
            var row = await _db.WhatsAppComplaints.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (row is null) return;
            row.CrmTicketId = crmTicketId;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("[Complaint-Repo] CRM ticket id={CrmId} saved for complaint={Id}", crmTicketId, id);
        }

        public async Task<WhatsAppComplaint?> GetByIdAsync(
            int id, CancellationToken ct = default) =>
            await _db.WhatsAppComplaints
                .Include(c => c.MediaItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<List<WhatsAppComplaint>> GetByPhoneAsync(
            string phone, int limit = 20, CancellationToken ct = default) =>
            await _db.WhatsAppComplaints
                .Where(c => c.WhatsappPhone == phone)
                .OrderByDescending(c => c.CreatedAt)
                .Take(Math.Clamp(limit, 1, 100))
                .AsNoTracking()
                .ToListAsync(ct);
    }
}
