using System.ComponentModel.DataAnnotations;
using ToDoList_Core.Domain.Enums;

namespace ToDoListAPI.DTOs.Duty
{
    public class RegisterDutyDTO
    {
        public int Id { get; set; }
        [Required]
        public string HeadLine { get; set; }
        public string Description { get; set; }
        public DutyStatus Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
    }
}
