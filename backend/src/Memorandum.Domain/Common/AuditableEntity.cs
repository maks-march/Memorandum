namespace Memorandum.Domain.Common;

/// <summary>
/// База всех сущностей: ключ и аудит. Идентификатор генерирует БД.
/// Время приходит снаружи: домен не читает системные часы, иначе момент нельзя
/// ни зафиксировать в тесте, ни повторно использовать в рамках одной операции.
/// </summary>
public abstract class AuditableEntity
{
    public const int UpdatedByMaxLength = 100;

    public long Id { get; protected set; }

    /// <summary>Момент создания, UTC.</summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>Момент последнего изменения, UTC.</summary>
    public DateTime UpdatedAt { get; protected set; }

    /// <summary>Кто создал или последним изменил запись. Имя пользователя, не его id.</summary>
    public string? UpdatedBy { get; protected set; }

    /// <param name="utcNow">Текущий момент UTC. Вызывающий сценарий получает его из <see cref="TimeProvider"/> один раз.</param>
    /// <param name="createdBy">Имя автора операции.</param>
    protected void MarkCreated(DateTime utcNow, string? createdBy)
    {
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
        UpdatedBy = Guard.Optional(createdBy, "Автор", UpdatedByMaxLength);
    }

    /// <param name="utcNow">Текущий момент UTC.</param>
    /// <param name="updatedBy">Имя автора операции; <c>null</c> оставляет прежнего.</param>
    protected void Touch(DateTime utcNow, string? updatedBy)
    {
        UpdatedAt = utcNow;

        var normalized = Guard.Optional(updatedBy, "Автор", UpdatedByMaxLength);
        if (normalized is not null)
            UpdatedBy = normalized;
    }
}
