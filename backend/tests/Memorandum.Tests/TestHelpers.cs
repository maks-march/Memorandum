using Memorandum.Domain.Common;

namespace Memorandum.Tests;

/// <summary>Идентификатор генерирует БД, поэтому в unit-тестах он проставляется рефлексией.</summary>
public static class TestHelpers
{
    public static T WithId<T>(this T entity, long id)
        where T : AuditableEntity
    {
        typeof(AuditableEntity)
            .GetProperty(nameof(AuditableEntity.Id))!
            .SetValue(entity, id);

        return entity;
    }
}
