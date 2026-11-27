using System.Text.Json;
using Memorandum.Domain.Common;

namespace Memorandum.Domain.Admin;

/// <summary>Действие, которое фиксируется в аудите.</summary>
public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Archived = 4,
    Verified = 5,
    XmlGenerated = 6,
    Imported = 7,
    Restored = 8,
}

/// <summary>
/// Запись аудита: кто и что сделал с сущностью.
/// </summary>
/// <remarks>
/// Append-only: записи не правятся и не удаляются, поэтому <c>Touch</c> нет —
/// время создания совпадает с моментом действия.
/// </remarks>
public sealed class AuditEntry : AuditableEntity
{
    public const int EntityMaxLength = 100;
    public const int ActionMaxLength = 50;

    private AuditEntry()
    {
    }

    public string Entity { get; private set; } = null!;

    public long? EntityId { get; private set; }

    public string Action { get; private set; } = null!;

    public long? UserId { get; private set; }

    public long? OrganizationId { get; private set; }

    /// <summary>Значения полей до и после: структура зависит от сущности.</summary>
    public string PayloadJson { get; private set; } = "{}";

    public static AuditEntry Create(
        string? entity,
        long? entityId,
        AuditAction action,
        DateTime utcNow,
        long? userId = null,
        long? organizationId = null,
        object? payload = null,
        string? createdBy = null)
    {
        var entry = new AuditEntry
        {
            Entity = Guard.Required(entity, "Сущность", EntityMaxLength),
            EntityId = entityId,
            Action = action.ToString(),
            UserId = userId,
            OrganizationId = organizationId,
            PayloadJson = payload is null
                ? "{}"
                : JsonSerializer.Serialize(payload, SerializerOptions),
        };

        entry.MarkCreated(utcNow, createdBy);
        return entry;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
