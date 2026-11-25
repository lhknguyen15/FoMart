using System.Linq.Expressions;
using FoMart.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

// Cài đặt chung của IRepositoryBase. <ChủĐề>Repository kế thừa lớp này.
public class RepositoryBase<TEntity>(FoMartDbContext dbContext) : IRepositoryBase<TEntity>
    where TEntity : class
{
    protected FoMartDbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

    public Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        DbSet.FindAsync([id], cancellationToken).AsTask();

    public Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        DbSet.SingleOrDefaultAsync(predicate, cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        DbSet.AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => DbSet.Update(entity);

    public void Delete(TEntity entity) => DbSet.Remove(entity);
}
