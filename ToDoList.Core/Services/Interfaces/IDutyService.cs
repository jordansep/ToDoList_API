using System.ComponentModel;
using System.Linq.Expressions;
using ToDoList_Core.Domain.Implementation;

namespace ToDoList_Core.Services.Interfaces
{
    public interface IDutyService
    {
        Duty BuildDuty(string headLine, string description);
        Task CreateDuty(Duty newDuty, int userId);
        Task<Duty> FindDuty(Expression<Func<Duty, bool>> predicate);
        Task<IEnumerable<Duty>> GetDutiesForUserAsync(int userId);
        // Task UpdateStatus(Duty updateDuty);
        Task DeleteDuty(Duty deleteDuty);
        Task UpdateDutyAsync(Duty dutyToUpdate);
    }
}
