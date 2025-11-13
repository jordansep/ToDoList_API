using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;

namespace ToDoList.Core.Domain.UseCases.Implementation
{
    public  class AssignUserRole
    {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IRepository<User> _userRepository;   
            public AssignUserRole(IRepository<User> repository, IUnitOfWork unitOfWork)
            {
                _userRepository = repository;
                _unitOfWork = unitOfWork;
            }
        public async Task AssignUserRoleToUser(User AssignToUser, UserRole role)
        {
            if (AssignToUser == null) throw new ArgumentNullException("User not found");
            AssignToUser.Role = role;
            _userRepository.Update(AssignToUser);

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
