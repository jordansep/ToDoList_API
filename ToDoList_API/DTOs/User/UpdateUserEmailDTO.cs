using System.ComponentModel.DataAnnotations;

namespace ToDoList_API.DTOs.User
{
    public class UpdateUserEmailDTO
    {
        [Required]
        public string NewEmail { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
