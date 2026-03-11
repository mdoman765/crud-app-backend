using crud_app_backend.DTOs;
using crud_app_backend.Models;
using crud_app_backend.Repositories;

namespace crud_app_backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        // In real project you would inject IJwtTokenService or similar
        // For demo we generate dummy token

        public AuthService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        //public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
        //{
        //    if (await _userRepo.UserExistsAsync(dto.Username, dto.Email))
        //    {
        //        return null; // or throw exception
        //    }

        //    var user = new User
        //    {
        //        Username = dto.Username,
        //        Email = dto.Email,
        //        Password = dto.Password,          // ← plain text (demo only!)
        //        IsActive = true,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    var created = await _userRepo.CreateAsync(user);

        //    return new AuthResponseDto
        //    {
        //        UserId = created.Id,
        //        Username = created.Username,
        //        Email = created.Email,
        //        Token = $"demo-jwt-{created.Id}-{Guid.NewGuid().ToString("N")[..8]}", // dummy
        //        ExpiresAt = DateTime.UtcNow.AddHours(24)
        //    };
        //}

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await _userRepo.GetByUsernameOrEmailAsync(dto.UsernameOrEmail);

            if (user == null || user.Password != dto.Password)
            {
                return null;
            }

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Token = $"demo-jwt-{user.Id}-{Guid.NewGuid().ToString("N")[..8]}", // dummy
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }
    }
}
