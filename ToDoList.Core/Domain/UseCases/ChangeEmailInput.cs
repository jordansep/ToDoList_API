using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Core.Domain.UseCases
{
    public class ChangeEmailInput
    {
        public string NewEmail { get; set; }
        public string Password { get; set; }
    }
}
