
namespace Memorandum.Api.Contracts.Projects;

/// <summary>Пояснительная записка проекта: <c>/api/v1/projects/{id}/documents</c>.</summary>
/// <param name="Id">Идентификатор документа.</param>
/// <param name="ProjectId">Проект, которому принадлежит документ.</param>
/// <param name="Title">Заголовок документа.</param>
/// <param name="SchemaCode">Код XML-схемы Минстроя.</param>
/// <param name="Status">Статус: Draft или Archived.</param>
/// <param name="IsArchived">Документ в архиве: только чтение.</param>
/// <param name="UpdatedAt">Момент последнего изменения.</param>
public sealed record ProjectDocumentHttpResponse(
    long Id,
    long ProjectId,
    string Title,
    string SchemaCode,
    string Status,
    bool IsArchived,
    DateTime UpdatedAt)
{
    /// <summary>Моковая версия для <c>/api/v1/mocks/projects/{id}/documents</c>.</summary>
    public static ProjectDocumentHttpResponse Mock(long projectId = 1, long id = 1) =>
        new(
            id,
            projectId,
            "Пояснительная записка",
            "01.07",
            "Draft",
            IsArchived: false,
            UpdatedAt: new DateTime(2026, 3, 1, 11, 30, 0, DateTimeKind.Utc));

    public static IReadOnlyList<ProjectDocumentHttpResponse> MockList(long projectId = 1) =>
        [Mock(projectId), Mock(projectId, 2) with { Title = "Пояснительная записка (редакция 2)" }];
}
