using crud_app_backend.DTOs;
using crud_app_backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace crud_app_backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var result = await _authService.LoginAsync(dto);

        //    if (result == null)
        //        return Unauthorized("Invalid username/email or password");

        //    return Ok(result);
        //}
        [HttpPost("login")]
        public  int Login(int shopcode)
        {
            int shop_id = 1234;

            return shop_id;
        }
    }
}
