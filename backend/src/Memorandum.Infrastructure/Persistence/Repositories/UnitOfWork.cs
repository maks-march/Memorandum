using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;
using Npgsql;

namespace Memorandum.Infrastructure.Persistence.Repositories;

/// <summary>
/// Граница сохранения. Ошибки уникальности провайдера превращаются в доменный конфликт,
/// чтобы Application не знал ни про Npgsql, ни про SQLite.
/// </summary>
public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryGetUniqueViolation(exception, out var constraint))
        {
            throw new ConflictException(CodeFor(constraint), MessageFor(constraint));
        }
    }

    private static bool TryGetUniqueViolation(DbUpdateException exception, out string constraint)
    {
        switch (exception.InnerException)
        {
            case PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres:
                constraint = postgres.ConstraintName ?? string.Empty;
                return true;

            case SqliteException { SqliteErrorCode: 19 } sqlite:
                // У SQLite имя индекса есть только в тексте сообщения.
                constraint = sqlite.Message;
                return true;

            default:
                constraint = string.Empty;
                return false;
        }
    }

    private static string CodeFor(string constraint) =>
        constraint.Contains("normalized_email", StringComparison.OrdinalIgnoreCase) ||
        constraint.Contains("users.normalized_email", StringComparison.OrdinalIgnoreCase)
            ? ErrorCodes.EmailDuplicate
            : ErrorCodes.ReferenceInUse;

    private static string MessageFor(string constraint) =>
        CodeFor(constraint) == ErrorCodes.EmailDuplicate
            ? "Пользователь с таким email уже зарегистрирован."
            : "Запись с таким значением уже существует.";
}
