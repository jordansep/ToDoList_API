// Ubicación: ToDoList.API/Controllers/UsersController.cs

// Importamos los "planos" (interfaces) y "entidades" de Core
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;
using ToDoListAPI.DTOs.User;
using AutoMapper;
// [ApiController] activa funciones automáticas de API (como validación de modelo)
[ApiController]
// [Route] define cómo llegar a este controlador.
// "api/[controller]" es un comodín que usa el nombre de la clase: "api/Users"
[Route("api/[controller]")]
public class UsersController : ControllerBase // Usa ControllerBase para APIs
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    // Agregamos el ILogger a la clase
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IMapper mapper, ILogger<UsersController> logger)
    {
        _userService = userService;
        _mapper = mapper;
        _logger = logger;
    }

    // --- PASO 2: Crear los Endpoints ("Los Botones") ---
    // [HttpPost] significa que este método responde a una petición HTTP POST.
    // El "Register" se añade a la ruta: POST /api/Users/Register
    [HttpPost("Register")]
    public async Task<IActionResult> HttpRegisterUser([FromBody] RegisterUserDTO newUser)
    {
        try
        {
            User createdUser = _mapper.Map<User>(newUser);

            await _userService.CreateUserAsync(createdUser);

            _logger.LogInformation("Usuario registrado: {@User}", createdUser);

            // Si todo va bien, devolvemos un "200 OK" con el usuario creado.
            return Ok(createdUser);
        }
        catch (Exception ex)
        {
            // Si el servicio lanza una excepción (ej. "el email ya existe")
            // la capturamos y devolvemos un "400 Bad Request".
            // (Luego mejoraremos este manejo de errores)
            return BadRequest(ex.Message);
        }
    }
    [HttpGet("SearchUser")]
    public async Task<ActionResult<User>> HttpSearchUser([FromQuery] string username)
    {
        try
        {
            var userFound = await _userService.FindUser(u => u.Username == username || u.Email == username );
            if (userFound == null)
            {
                return NotFound($"No se encontró ningún usuario con el nombre o email: {username}");
            }
            return Ok(userFound);
        }
        catch(Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpPut("UpdateUser")]
    public async Task<IActionResult> HttpUpdateUserAsync(int id,[FromBody]UpdateUserNormalContentDTO user)
    {
        try
        {
            var userToUpdate = await _userService.FindUser(u => u.Id == id);
            if (userToUpdate == null) return BadRequest("No lo juno");
            _mapper.Map(user, userToUpdate);
            await _userService.UpdateUser(userToUpdate);
            return Ok(userToUpdate);
        }
        catch(Exception ex)
        {
            return BadRequest("Mal ahi amigo");
        }
    }
    [HttpDelete("DeleteUser")]
    public async Task<IActionResult> HttpDeleteUserAsync(int id) {
        try
        {
            var userToDelete = await _userService.FindUser(u => u.Id == id);
            await _userService.DeleteUser(userToDelete);
            return Ok("Eliminado con exito");
        } catch (Exception ex)
        {
            return BadRequest($"Hubo un problema: {ex}");
        }
    }

    // Aquí podrías añadir tu endpoint de Login
    // [HttpPost("Login")]
    // public async Task<IActionResult> Login([FromBody] LoginDto login)
    // {
    //    var token = await _authService.LoginAsync(login.Email, login.Password);
    //    if (token == null) return Unauthorized();
    //    return Ok(new { Token = token });
    // }
}