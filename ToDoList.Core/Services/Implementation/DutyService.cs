using System.Linq.Expressions;
using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Domain.UseCases.Implementation;
using ToDoList_Core.Services.Interfaces;

namespace ToDoList_Core.Services.Implementation
{
    public class DutyService : IDutyService
    {
        protected readonly IRepository<Duty> _repository;
        protected readonly IUnitOfWork _unitOfWork;
        public DutyService(IRepository<Duty> repository, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
        }

        public Duty BuildDuty(string headLine, string description)
        {
            return new Duty {
                HeadLine = headLine,
                Description = description,
                StartDate = DateTime.Now
            };
        }

        public async Task CreateDuty(Duty newDuty)
        {
            _repository.Add(newDuty);
            await _unitOfWork.SaveChangesAsync();
        }
        //public async Task UpdateStatus(Duty duty, DutyStatus newStatus)
        //{
        //    ChangeStatus(duty, newStatus);
        //    await _unitOfWork.SaveChangesAsync();
        //}

        public async Task DeleteDuty(Duty deleteDuty)
        {
            _repository.Delete(deleteDuty);
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task<Duty> FindDuty(Expression<Func<Duty, bool>> predicate)
        {
            return
                await _repository.FindAsync(predicate);
        }
        public async Task<IEnumerable<Duty>> GetDutiesForUserAsync(int userId)
        {
            return 
                await _repository.FindAllAsync(d => d.UserID == userId);
        }
        public async Task UpdateDutyAsync(Duty dutyToUpdate)
        {
            _repository.Update(dutyToUpdate);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
