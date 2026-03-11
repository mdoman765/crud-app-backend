namespace crud_app_backend.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // For learning/demo only – NEVER store plain password in production!
        public string Password { get; set; } = string.Empty;

       

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional fields you can add later
        // public string? FullName { get; set; }
        // public string? PhoneNumber { get; set; }
        // public DateTime? LastLoginAt { get; set; }
    }
}
