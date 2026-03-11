using crud_app_backend.DTOs;

namespace crud_app_backend.Services
{
    public interface IAuthService
    {
      //  Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    }
}
