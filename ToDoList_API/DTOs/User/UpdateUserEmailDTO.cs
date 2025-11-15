using System.ComponentModel.DataAnnotations;

namespace ToDoListAPI.DTOs.User
{
    public class UpdateUserEmailDTO
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
