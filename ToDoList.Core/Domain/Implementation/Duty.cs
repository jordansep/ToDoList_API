using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Domain.Interfaces;

namespace ToDoList_Core.Domain.Implementation
{
    public class Duty : IDuty

    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id {  get; set; }
        [Required]
        public string HeadLine { get; set; }
        public string Description {  get; set; }
        [Required]
        public DutyStatus Status { get; set; }
        public DateTime FinishDate { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        public int UserID { get; set; }
        public User User {  get; set; }
        public Duty() { }
        public string ReadDescription(string description)
        {
            return description;
        }

        public string ReadHeadLine(string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException("Ingrese un titulo para su tarea");
            }
            return title;
        }
    }
}
