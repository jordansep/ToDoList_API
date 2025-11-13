using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoList_Core.Domain.Interfaces
{
    internal interface IDuty
    {
        string ReadHeadLine(string title);
        string ReadDescription(string description);
    }
}
