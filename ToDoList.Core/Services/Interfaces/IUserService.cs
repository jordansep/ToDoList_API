using System.Linq.Expressions;
using ToDoList_Core.Domain.Implementation;

namespace ToDoList_Core.Services.Interfaces
{
     public interface IUserService
    {
        Task<User> CreateUserAsync(User newUser);
        Task DeleteUser(User deleteUser);
        Task<User> FindUser(Expression<Func<User, bool>> predicate);
        Task UpdateUser(User updateUser);
        string HashPassword(User HashPasswordToUser);
    }
}