using Memorandum.Domain.Common;

namespace Memorandum.Domain.Interfaces;

/// <summary>Общий CRUD. Запросы под сценарий описываются в специализированных интерфейсах.</summary>
public interface IRepository<T> where T : AuditableEntity
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Remove(T entity);
}
