using crud_app_backend.DTOs;

namespace crud_app_backend.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();

        Task<IEnumerable<SubCategoryDto>> GetSubCategoriesAsync(int category);
        Task<List<ProductResponseDto>> GetProductsByCategorySubCategoryAsync(int subCategory); // Assume ProductResponseDto exists from your original code

        Task<OrderResponseDto> CreateOrderByUserAndSubCategoryAsync(CreateOrderDto dto);
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(int userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);

        Task<FeedbackResponseDto> SubmitFeedbackAsync(SubmitFeedbackDto dto);
        Task<List<FeedbackResponseDto>> GetFeedbackByUserIdAsync(int userId);
    }
}
