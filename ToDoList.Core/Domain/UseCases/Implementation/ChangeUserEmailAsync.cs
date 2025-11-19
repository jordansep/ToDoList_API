using System;
using System.Collections.Generic;
using System.Text;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;

namespace ToDoList.Core.Domain.UseCases.Implementation
{
    public class ChangeUserEmailAsync
    {
        private readonly IRepository<User> _repository;
        private readonly IUnitOfWork _unitOfWork;
        public ChangeUserEmailAsync(
            IRepository<User> repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public async Task Execute(int userId, ChangeEmailInput input)
        {
            var user = await _repository.FindAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("Usuario no encontrado");
            if (!BCrypt.Net.BCrypt.EnhancedVerify(input.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("La contraseña no coincide");
            }
            var emailTaken  = await _repository.FindAsync(u => u.Email == input.NewEmail);
            if (emailTaken != null) throw new InvalidOperationException("El Email ya esta en uso");
            user.Email = input.NewEmail;
            _repository.Update(user);
            await _unitOfWork.SaveChangesAsync();


        }
    }
}
