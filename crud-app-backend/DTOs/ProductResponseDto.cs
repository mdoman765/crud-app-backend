namespace crud_app_backend.DTOs
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Optional – if you want subcategory/category info in responses
        public int SubcategoryId { get; set; }
        public string? SubcategoryName { get; set; }
        

        // Add any other fields you return in product listings
    }
}
