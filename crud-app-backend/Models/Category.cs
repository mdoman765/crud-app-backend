namespace crud_app_backend.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
