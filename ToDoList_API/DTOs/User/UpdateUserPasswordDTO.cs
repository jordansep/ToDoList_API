using System.ComponentModel.DataAnnotations;

namespace ToDoListAPI.DTOs.User
{
    public class UpdateUserPasswordDTO
    {
        [Required]
        [MinLength (6)]
        public string OldPassword {  get; set; }
        [Required]
        [MinLength (6)]
        public string NewPassword { get; set; }
        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }

    }
}
