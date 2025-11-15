using System.ComponentModel.DataAnnotations;

namespace ToDoListAPI.DTOs.User
{
    public class RegisterUserDTO
    {
        [Required]
        [MinLength (3)]
        public string Username { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }
}
