using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;
using ToDoListAPI.DTOs.User;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ToDoList_Core.Domain.Enums;
using ToDoList_API.Authorization.RuleHandler;
using ToDoList_API.Extensions;
using ToDoList.Core.Domain.UseCases.Implementation;
using ToDoList.Core.Domain.UseCases;
using ToDoList_API.DTOs.User;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IMapper mapper, ILogger<UsersController> logger)
    {
        _userService = userService;
        _mapper = mapper;
        _logger = logger;
    }
    [HttpPost("Register")]
    [AllowAnonymous]
    public async Task<IActionResult> HttpRegisterUser([FromBody] RegisterUserDTO newUser)
    {
        try
        {
            User createdUser = _mapper.Map<User>(newUser);

            await _userService.CreateUserAsync(createdUser);

            _logger.LogInformation("Usuario registrado: {@User}", createdUser);
            return Ok(createdUser);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("SearchUser")]
    [Authorize(Roles = "Admin, Manager")]
    public async Task<ActionResult<User>> HttpSearchUser([FromQuery] string username)
    {
        try
        {
            var userFound = await _userService.FindUser(u => u.Username == username || u.Email == username );
            if (userFound
                == null)
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

    [HttpPut("UpdateUser/{id}")]
    [Authorize(Policy = "IsOwnerOrAdmin")]
    public async Task<IActionResult> HttpUpdateUserAsync(int id, [FromBody]UpdateUserNormalContentDTO user)
    {
        try
        {
            var userToUpdate = await _userService.FindUser(u => u.Id == id);
            if (userToUpdate == null) return NotFound($"Usuario con id {id} no encontrado");
            _mapper.Map(user, userToUpdate);
            await _userService.UpdateUser(userToUpdate);
            return Ok(userToUpdate);
        }
        catch(Exception ex)
        {
            return BadRequest($"Error al actualizar usuario: {ex.Message}");
        }
    }

    [HttpPut("UpdatePassword/{id}")]
    [Authorize(Policy = "IsOwnerOrAdmin")]
    public async Task<IActionResult> HttpUpdatePasswordAsync(
        int id,
        [FromBody] UpdateUserPasswordDTO userPasswords,
        [FromServices] ChangePasswordAsync changePasswordUseCase)
    {
        var passwordInput = new ChangePasswordInput
        {
            OldPassword = userPasswords.OldPassword,
            NewPassword = userPasswords.NewPassword
        };
        try
        {
            await changePasswordUseCase.Execute(id, passwordInput);
            return Ok("Contraseña Actualizada");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error al actualizar la Contraseña: {ex.Message}");
        }
    }

    [HttpPut("UpdateEmail/{id}")]
    [Authorize(Policy = "IsOwnerOrAdmin")]
    public async Task<IActionResult> HttpUpdateEmailAsync(
        int id,
        [FromBody] UpdateUserEmailDTO userEmails,
        [FromServices] ChangeUserEmailAsync changeEmailUseCase
        )
    {
        var newEmail = new ChangeEmailInput
        {
            NewEmail = userEmails.NewEmail,
            Password = userEmails.Password,
        };
        try
        {
            await changeEmailUseCase.Execute(id, newEmail);
            return Ok("Email actualizado con exito");
        }
        catch (Exception ex)
        {
            return BadRequest($"Hubo un problema: {ex.Message}");
        }
    }
    [HttpDelete("DeleteUser/{id}")]
    [Authorize(Policy = "IsOwnerOrAdmin")]
    public async Task<IActionResult> HttpDeleteUserAsync(int id) {
        try
        {
            var userToDelete = await _userService.FindUser(u => u.Id == id);
            if (userToDelete == null) return NotFound($"Usuario con id {id} no encontrado");
            await _userService.DeleteUser(userToDelete);
            return Ok("Eliminado con exito");
        } catch (Exception ex)
        {
            return BadRequest($"Hubo un problema: {ex.Message}");
        }
    }
}