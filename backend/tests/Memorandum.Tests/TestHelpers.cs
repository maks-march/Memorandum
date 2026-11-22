using Memorandum.Domain.Common;

namespace Memorandum.Tests;

/// <summary>Общие значения для тестов домена.</summary>
public static class TestHelpers
{
    /// <summary>Фиксированный момент времени — вместо системных часов.</summary>
    public static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>Момент позже <see cref="Now"/>: для проверки UpdatedAt после изменения.</summary>
    public static readonly DateTime Later = Now.AddHours(2);

    /// <summary>Идентификатор генерирует БД, поэтому в unit-тестах он проставляется рефлексией.</summary>
    public static T WithId<T>(this T entity, long id)
        where T : AuditableEntity
    {
        typeof(AuditableEntity)
            .GetProperty(nameof(AuditableEntity.Id))!
            .SetValue(entity, id);

        return entity;
    }
}
