using crud_app_backend.Models;

namespace crud_app_backend.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<User> CreateAsync(User user);
        Task<bool> UserExistsAsync(string username, string email);
        // Add more methods later: Update, Delete, etc.
    }
}
