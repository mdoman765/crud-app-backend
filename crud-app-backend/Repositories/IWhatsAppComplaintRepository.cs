using crud_app_backend.Models;

namespace crud_app_backend.Repositories
{
    public interface IWhatsAppComplaintRepository
    {
        Task<WhatsAppComplaint> InsertAsync(WhatsAppComplaint complaint, CancellationToken ct = default);
        Task UpdateComplaintNumberAsync(int id, string number, CancellationToken ct = default);
        Task InsertMediaAsync(WhatsAppComplaintMedia media, CancellationToken ct = default);
        Task<WhatsAppComplaint?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<WhatsAppComplaint>> GetByPhoneAsync(string phone, int limit = 20, CancellationToken ct = default);
        Task UpdateCrmTicketIdAsync(int id, string crmTicketId, CancellationToken ct = default);
    }
}
