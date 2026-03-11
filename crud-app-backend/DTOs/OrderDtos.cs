namespace crud_app_backend.DTOs
{
    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public int SubCategoryId { get; set; }           // main filtering category
        public int? ProductId { get; set; }              // optional - if specific product
        public int Quantity { get; set; }
        //public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
       // public string PaymentMethod { get; set; } = "cash"; // cash, mobile, card, etc.
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubCategoryId { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // populated in service
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";     // Pending, Confirmed, Shipped, Delivered, Cancelled
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubmitFeedbackDto
    {
        public int UserId { get; set; } 
        public string? Comment { get; set; }
    }

    public class FeedbackResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
