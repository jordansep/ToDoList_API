namespace ToDoList_Core.Services.Interfaces
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}