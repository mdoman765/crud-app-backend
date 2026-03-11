using crud_app_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace crud_app_backend.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(
            string? search, bool? isActive)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)));

            //if (!string.IsNullOrWhiteSpace(category))
            //    query = query.Where(p => p.Category == category);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id) =>
            await _context.Products.FindAsync(id);

        public async Task<Product> CreateAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> UpdateAsync(int id, Product product)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return null;

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
           
            existing.Stock = product.Stock;
            existing.IsActive = product.IsActive;
            existing.ImageUrl = product.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        //public async Task<IEnumerable<string>> GetCategoriesAsync() =>
        //     await _context.Categories.Select(c=>c.Name).OrderBy(name=>name).ToListAsync();
        public async Task<IEnumerable<object>> GetCategoriesAsync()
        {
            return await _context.Categories
                .Select(c => new { c.Id, c.Name })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
