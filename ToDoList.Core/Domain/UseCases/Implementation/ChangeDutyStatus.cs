using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;

namespace ToDoList_Core.Domain.UseCases.Implementation
{
    public class ChangeDutyStatus
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Duty> _dutyRepository;
        public ChangeDutyStatus(IRepository<Duty> repository, IUnitOfWork unitOfWork)
        {
            _dutyRepository = repository;
            _unitOfWork = unitOfWork;
        }
        public async Task ChangeStatus(Duty updateDutyStatus, DutyStatus newStatus) {
          
            
            updateDutyStatus.Status = newStatus;
            _dutyRepository.Update(updateDutyStatus);
            await _unitOfWork.SaveChangesAsync();
        }
        
    }
}
