using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Domain.Interfaces;

namespace ToDoList_Core.Domain.Implementation
{
    public class User : IUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Username { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public List<Duty> Duties { get; set; }
        public UserRole Role { get; set; }
        public DateTime UserCreatedDate { get; set; }
        public User() {
            Duties = new List<Duty>();
            Role = 0;
        }
        public string ReadEmail(string email)
        {
            if(email == null) throw new ArgumentNullException("Ingrese un email por favor");
            if (!email.Contains("@")) throw new ArgumentException("Su mail debe contener @");
            return email;
        }

        public string ReadLastName(string lastName)
        {
            if(lastName == null) throw new ArgumentNullException("Ingrese un apellido");
                return lastName;
        }

        public string ReadName(string name)
        {
            if(name == null) throw new ArgumentNullException("Ingrese un nombre");
            return name;
        }

        public string ReadPassword(string password)
        {
            if (password == null) throw new ArgumentNullException("Ingrese su contraseña");
            return password;
        }

    }
}
