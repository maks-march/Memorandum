namespace Memorandum.Domain.Interfaces;

/// <summary>Граница сохранения: один вызов — одна транзакция.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
