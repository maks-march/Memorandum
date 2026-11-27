namespace Memorandum.Api.Contracts.Admin;

/// <summary>Запись аудита для <c>/api/v1/admin/audit</c>.</summary>
/// <param name="Id">Идентификатор записи.</param>
/// <param name="Entity">С каким объектом работали.</param>
/// <param name="EntityId">Идентификатор объекта.</param>
/// <param name="Action">Действие: Created, Updated, Archived, Verified и т. д.</param>
/// <param name="UserName">Кто выполнил.</param>
/// <param name="OrganizationName">Организация, в которой выполнено действие.</param>
/// <param name="CreatedAt">Когда выполнено.</param>
public sealed record AuditEntryHttpResponse(
    long Id,
    string Entity,
    long? EntityId,
    string Action,
    string? UserName,
    string? OrganizationName,
    DateTime CreatedAt)
{
    /// <summary>Моковая запись аудита.</summary>
    public static AuditEntryHttpResponse Mock(long id = 1) =>
        new(
            id,
            "Document",
            EntityId: 1,
            "Verified",
            "Иванов Иван Иванович",
            "ООО «Проект»",
            new DateTime(2026, 9, 4, 15, 0, 0, DateTimeKind.Utc));

    /// <summary>Моковый список событий.</summary>
    public static IReadOnlyList<AuditEntryHttpResponse> MockList() =>
        [
            Mock(1),
            Mock(2) with { Entity = "SchemaVersion", EntityId = 3, Action = "Updated" },
            Mock(3) with { Entity = "Organization", EntityId = 1, Action = "Created" },
        ];
}
