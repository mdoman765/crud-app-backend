using CrudApp.API.DTOs;

namespace crud_app_backend.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync(string? search,  bool? isActive);
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto?> UpdateAsync(int id, UpdateProductDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<object>> GetCategoriesAsync();
    }
}
