using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoList_Core.Domain.Interfaces
{
    public interface IUser
    {
        string ReadName(string name);
        string ReadLastName(string lastName);
        string ReadEmail(string email);
        string ReadPassword(string password);

    }
}
