using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ToDoList_Core.Services.Interfaces;
namespace ToDoList_Infrastructure.Server.Implementation
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDBContext _context;
        protected readonly DbSet<T> _dbSet;
        public Repository(AppDBContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public void Add(T entity)
            => _dbSet.Add(entity);
        public void Delete(T entity)
            =>  _dbSet.Remove(entity);
        public void Update(T entity)
            => _dbSet.Update(entity);
        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes){
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            query = query.Where(predicate);
            return await query.ToListAsync();
        }
        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate);
    }
}
