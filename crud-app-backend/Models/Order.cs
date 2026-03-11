namespace crud_app_backend.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubCategoryId { get; set; }
        public int? ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "cash";
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation (optional)
        public Product? Product { get; set; }
    }
    public class Feedback
    {
        public int Id { get; set; }                 // primary key
        public int UserId { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
