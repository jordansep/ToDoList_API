using System;
using System.Collections.Generic;
using System.Text;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;

namespace ToDoList.Core.Domain.UseCases.Implementation
{
    public class ChangePasswordAsync
    {
        private readonly IRepository<User> _repository;
        private readonly IUnitOfWork _unitOfWork;
        public ChangePasswordAsync(
            IRepository<User> repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public async Task Execute(int userId, ChangePasswordInput inputPassword)
        {
            var user = await _repository.FindAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("Usuario no encontrado");
            if (!BCrypt.Net.BCrypt.EnhancedVerify(inputPassword.OldPassword, user.PasswordHash)) { 
                throw new KeyNotFoundException("La Contraseña anterior es incorrecta");
            }
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(inputPassword.NewPassword);
            user.PasswordHash = newPasswordHash;
            _repository.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
