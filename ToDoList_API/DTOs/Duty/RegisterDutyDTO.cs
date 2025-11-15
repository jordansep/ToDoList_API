using System.ComponentModel.DataAnnotations;

namespace ToDoListAPI.DTOs.Duty
{
    public class RegisterDutyDTO
    {
        [Required]
        public string HeadLine { get; set; }
        public string Description { get; set; }
    }
}
