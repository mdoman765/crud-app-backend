using crud_app_backend.DTOs;
using crud_app_backend.Models;

namespace crud_app_backend.Repositories
{
    // IOrderRepository.cs
    public interface IOrderRepository
    {
        Task<List<Order>> GetOrdersByUserIdAsync(int userId);
        Task<Order?> GetByIdAsync(int id);
        Task<Order> CreateAsync(Order order);
        Task<bool> UpdateStatusAsync(int orderId, string newStatus);
        Task<bool> DeleteAsync(int id);

        // For discovery endpoints
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<List<SubCategoryDto>> GetSubCategoriesAsync(int category);
        Task<List<Product>> GetProductsByCategoryAndSubCategoryAsync(int subCategory);
    }

    // OrderRepository.cs (implementation similar to ProductRepository)
}
