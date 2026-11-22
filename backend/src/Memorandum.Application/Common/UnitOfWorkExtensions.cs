using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;

namespace Memorandum.Application.Common;

internal static class UnitOfWorkExtensions
{
    /// <summary>
    /// Сохраняет изменения, переводя доменный конфликт из Infrastructure в Failure.
    /// Остальные исключения пробрасываются: БД недоступна — это 500, а не 409.
    /// </summary>
    public static async Task<Result<int>> TrySaveAsync(
        this IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Result<int>.Success(await unitOfWork.SaveChangesAsync(cancellationToken));
        }
        catch (ConflictException exception)
        {
            return Result<int>.Failure(Error.FromDomainException(exception));
        }
    }
}
