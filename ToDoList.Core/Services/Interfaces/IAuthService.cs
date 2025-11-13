using ToDoList_Core.Domain.Implementation;

namespace ToDoList_Core.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(string username, string password);
    }
}