using System.ComponentModel.DataAnnotations;

namespace ToDoListAPI.DTOs.User
{
    public class UpdateUserPasswordDTO
    {
        [Required]
        [MinLength (6)]
        public string password {  get; set; }
    }
}
