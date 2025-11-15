using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ToDoList_Core.Services.Interfaces;
using ToDoListAPI.DTOs.Auth;

namespace ToDoListAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("Login")]
        public async Task<IActionResult> HttpLoginAsync([FromBody] LoginDTO loginDTO)
        {
            try
            {
                var token = await _authService.LoginAsync(loginDTO.Username, loginDTO.Password);
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("Usuario o contraseña inválidos.");
                }
                return Ok(token);
            }
            catch (Exception ex) {
                return BadRequest($"Error: {ex}");
            }

        }
    }
}
