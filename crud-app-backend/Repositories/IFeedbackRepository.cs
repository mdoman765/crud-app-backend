using crud_app_backend.Models;

namespace crud_app_backend.Repositories
{
    // IFeedbackRepository.cs
    public interface IFeedbackRepository
    {
        Task<Feedback> CreateAsync(Feedback feedback);
        //Task<List<Feedback>> GetFeedbackByOrderIdAsync(int orderId);
        Task<List<Feedback>> GetFeedbackByUserIdAsync(int userId);
    }
}
