using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Core.Domain.UseCases
{
    public class ChangePasswordInput
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }

    }
}
