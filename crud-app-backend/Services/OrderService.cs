using crud_app_backend.DTOs;
using crud_app_backend.Models;
using crud_app_backend.Repositories;

namespace crud_app_backend.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IFeedbackRepository _feedbackRepo;

        public OrderService(IOrderRepository orderRepo, IFeedbackRepository feedbackRepo)
        {
            _orderRepo = orderRepo;
            _feedbackRepo = feedbackRepo;
        }

public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
{
    return await _orderRepo.GetAllCategoriesAsync();
}

       
        public async Task<IEnumerable<SubCategoryDto>> GetSubCategoriesAsync(int category)
        {
            return await _orderRepo.GetSubCategoriesAsync(category);
        }

        public async Task<List<ProductResponseDto>> GetProductsByCategorySubCategoryAsync(int subCategory)
        {
            var products = await _orderRepo.GetProductsByCategoryAndSubCategoryAsync(subCategory);
            // Map to ProductResponseDto (assume you have a mapper or manual mapping)
            return products.Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name, 
            }).ToList();
        }

        public async Task<OrderResponseDto> CreateOrderByUserAndSubCategoryAsync(CreateOrderDto dto)
        {
            var order = new Order
            {
                UserId = dto.UserId,
                SubCategoryId = dto.SubCategoryId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                //TotalAmount = dto.TotalAmount,
                //PaymentMethod = dto.PaymentMethod,
                Notes = dto.Notes
            };

            var createdOrder = await _orderRepo.CreateAsync(order);

            // Optionally load product name
            string productName = string.Empty;
            if (createdOrder.Product != null)
            {
                productName = createdOrder.Product.Name;
            }

            return new OrderResponseDto
            {
                Id = createdOrder.Id,
                UserId = createdOrder.UserId,
                SubCategoryId = createdOrder.SubCategoryId,
                ProductId = createdOrder.ProductId,
                ProductName = productName,
                Quantity = createdOrder.Quantity,
                TotalAmount = createdOrder.TotalAmount,
                Status = createdOrder.Status,
                PaymentMethod = createdOrder.PaymentMethod,
                Notes = createdOrder.Notes,
                CreatedAt = createdOrder.CreatedAt
            };
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return null;

            string productName = order.Product?.Name ?? string.Empty;

            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                SubCategoryId = order.SubCategoryId,
                ProductId = order.ProductId,
                ProductName = productName,
                Quantity = order.Quantity,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt
            };
        }

        public async Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _orderRepo.GetOrdersByUserIdAsync(userId);
            return orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                UserId = o.UserId,
                SubCategoryId = o.SubCategoryId,
                ProductId = o.ProductId,
                ProductName = o.Product?.Name ?? string.Empty,
                Quantity = o.Quantity,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                Notes = o.Notes,
                CreatedAt = o.CreatedAt
            }).ToList();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            return await _orderRepo.UpdateStatusAsync(orderId, status);
        }

        public async Task<FeedbackResponseDto> SubmitFeedbackAsync(SubmitFeedbackDto dto)
        {
            var feedback = new Feedback
            {
                UserId = dto.UserId,
               
                Comment = dto.Comment
            };

            var createdFeedback = await _feedbackRepo.CreateAsync(feedback);

            return new FeedbackResponseDto
            {
                
                UserId = createdFeedback.UserId,
                Comment = createdFeedback.Comment,
                CreatedAt = createdFeedback.CreatedAt
            };
        }

        public async Task<List<FeedbackResponseDto>> GetFeedbackByUserIdAsync(int userId)
        {
            var feedbacks = await _feedbackRepo.GetFeedbackByUserIdAsync(userId);
            return feedbacks.Select(f => new FeedbackResponseDto
            {
                UserId = f.UserId,
               Comment = f.Comment,
                CreatedAt = f.CreatedAt
            }).ToList();
        }
    }
}
