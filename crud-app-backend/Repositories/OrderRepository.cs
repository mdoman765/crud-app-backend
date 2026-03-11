using crud_app_backend.DTOs;
using crud_app_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace crud_app_backend.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context; // Assume your DbContext

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Product) // Optional
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> UpdateStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;
            order.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }


        public async Task<List<SubCategoryDto>> GetSubCategoriesAsync(int category)
        {
            return await _context.SubCategories
                .Where(c => c.CategoryId == category) // filter by category
                .Select(c => new SubCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }


        public async Task<List<Product>> GetProductsByCategoryAndSubCategoryAsync(int subCategoryId)
        {
            try
            {
                return await _context.Products
                    .Where(p => p.SubcategoryId == subCategoryId)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error fetching products: {ex.Message}");
                // or: _logger.LogError(ex, "Failed to get products for subcategory {Id}", subCategoryId);

                throw; // rethrow so controller can return 500 or custom error
            }
        }
    }

    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly AppDbContext _context;

        public FeedbackRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Feedback> CreateAsync(Feedback feedback)
        {
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return feedback;
        }

        //public async Task<List<Feedback>> GetFeedbackByOrderIdAsync(int orderId)
        //{
        //    return await _context.Feedbacks
        //        .Where(f => f.OrderId == orderId)
        //        .ToListAsync();
        //}

        public async Task<List<Feedback>> GetFeedbackByUserIdAsync(int userId)
        {
            try
            {
                return await _context.Feedbacks
                    .Where(f => f.UserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching feedback for user {userId}: {ex.Message}");
                // optional: _logger.LogError(ex, "Error fetching feedback for user {UserId}", userId);

                throw; // rethrow so controller/service can handle the error
            }
        }
    }
}
