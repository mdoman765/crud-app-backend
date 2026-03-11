namespace crud_app_backend.DTOs
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;   // JWT or session token
        public DateTime ExpiresAt { get; set; }
    }
}
