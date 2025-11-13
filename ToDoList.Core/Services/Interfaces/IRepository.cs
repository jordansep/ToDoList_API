using System.Linq.Expressions;

namespace ToDoList_Core.Services.Interfaces
{
    public interface IRepository<T> where T : class
    {
        void Delete(T entity);
        Task<T> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        void Update(T entity);
        void Add(T entity);
    }
}