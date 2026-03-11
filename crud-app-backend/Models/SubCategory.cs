namespace crud_app_backend.Models
{
    public class SubCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }     // navigation

        // Navigation to products
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
