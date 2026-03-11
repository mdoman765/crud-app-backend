using crud_app_backend.Models;
using crud_app_backend.Repositories;
using CrudApp.API.DTOs;

namespace crud_app_backend.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo) => _repo = repo;

        public async Task<IEnumerable<ProductDto>> GetAllAsync(
            string? search, bool? isActive)
        {
            var products = await _repo.GetAllAsync(search, isActive);
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var p = await _repo.GetByIdAsync(id);
            return p == null ? null : MapToDto(p);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
               // Category = dto.Category,
                Stock = dto.Stock,
                IsActive = dto.IsActive,
                ImageUrl = dto.ImageUrl
            };
            var created = await _repo.CreateAsync(product);
            return MapToDto(created);
        }

        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
              //  Category = dto.Category,
                Stock = dto.Stock,
                IsActive = dto.IsActive,
                ImageUrl = dto.ImageUrl
            };
            var updated = await _repo.UpdateAsync(id, product);
            return updated == null ? null : MapToDto(updated);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

        public Task<IEnumerable<object>> GetCategoriesAsync() => _repo.GetCategoriesAsync();

        private static ProductDto MapToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            //Category = p.Category,
            Stock = p.Stock,
            IsActive = p.IsActive,
            ImageUrl = p.ImageUrl,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
