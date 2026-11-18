using Microsoft.EntityFrameworkCore;
using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;

namespace Memorandum.Infrastructure.Persistence.Repositories;

/// <summary>Общий CRUD поверх <see cref="AppDbContext"/>.</summary>
public abstract class EfRepository<T>(AppDbContext db) : IRepository<T>
    where T : AuditableEntity
{
    protected AppDbContext Db { get; } = db;

    protected DbSet<T> Set => Db.Set<T>();

    public virtual Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual void Remove(T entity) => Set.Remove(entity);
}
