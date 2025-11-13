using System.Linq.Expressions;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;
using BCrypt.Net;
using System.Net.Sockets;

namespace ToDoList_Core.Services.Implementation
{
    public class UserService : IUserService
    {
        protected readonly IRepository<User> _repository;
        protected readonly IUnitOfWork _unitOfWork;
        public UserService(IRepository<User> repository, IUnitOfWork unitOfWork) {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public string HashPassword(User user)
        {
            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new ArgumentNullException(nameof(user.PasswordHash), "Password cannot be null or empty.");

            return BCrypt.Net.BCrypt.EnhancedHashPassword(user.PasswordHash);
        }
        public async Task<User> CreateUserAsync(User newUser)
        {
            newUser.PasswordHash = HashPassword(newUser);
            _repository.Add(newUser);
            await _unitOfWork.SaveChangesAsync();
            return newUser;
        }
        public async Task UpdateUser(User updateUser) {
            _repository.Update(updateUser);
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task DeleteUser(User deleteUser) {
            _repository.Delete(deleteUser);
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task<User> FindUser(Expression<Func<User, bool>> predicate) 
            => await _repository.FindAsync(predicate);


    }
}
