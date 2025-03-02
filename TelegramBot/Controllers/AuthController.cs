using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace TelegramBot.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public AuthController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] string username)
        {
            var token = _jwtService.GenerateToken(username);
            return Ok(new { Token = token });
        }
    }
}
