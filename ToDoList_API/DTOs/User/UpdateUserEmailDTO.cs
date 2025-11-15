using System.ComponentModel.DataAnnotations;

namespace ToDoList_API.DTOs.User
{
    public class UpdateUserEmailDTO
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
