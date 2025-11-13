using ToDoList_Core.Services.Interfaces;

namespace ToDoList_Infrastructure.Server.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        public readonly AppDBContext _context;
        public UnitOfWork(AppDBContext context) { _context = context; }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
