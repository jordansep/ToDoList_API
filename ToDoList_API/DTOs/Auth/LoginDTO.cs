using System.ComponentModel.DataAnnotations;

namespace ToDoListAPI.DTOs.Auth
{
    public class LoginDTO
    {
        [Required]
        [MinLength (3)]
        public string Username { get; set; }
        [Required]
        [MinLength (6)]
        public string Password { get; set; }
    }
}
