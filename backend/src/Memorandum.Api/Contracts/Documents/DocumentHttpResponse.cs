namespace Memorandum.Api.Contracts.Documents;

/// <summary>Пояснительная записка в списке: <c>/api/v1/documents</c>.</summary>
/// <param name="Id">Идентификатор документа.</param>
/// <param name="ProjectId">Проект документа.</param>
/// <param name="ProjectName">Наименование проекта для колонки «Проект».</param>
/// <param name="Title">Заголовок документа.</param>
/// <param name="OrganizationName">Организация, которой принадлежит документ.</param>
/// <param name="Author">Автор последней правки.</param>
/// <param name="Status">draft, verified, has_errors или archived.</param>
/// <param name="SchemaCode">Код XML-схемы Минстроя.</param>
/// <param name="UpdatedAt">Момент последнего изменения.</param>
public sealed record DocumentHttpResponse(
    long Id,
    long ProjectId,
    string ProjectName,
    string Title,
    string OrganizationName,
    string? Author,
    string Status,
    string SchemaCode,
    DateTime UpdatedAt)
{
    /// <summary>Моковая версия для <c>/api/v1/mocks/documents</c>.</summary>
    public static DocumentHttpResponse Mock(long id = 1, long projectId = 1) =>
        new(
            id,
            projectId,
            "Жилой комплекс № 1",
            "Пояснительная записка",
            "ООО «Проект»",
            "Иванов Иван Иванович",
            "draft",
            "01.07",
            new DateTime(2026, 9, 4, 14, 20, 0, DateTimeKind.Utc));

    /// <summary>Моковый список: все четыре статуса.</summary>
    public static IReadOnlyList<DocumentHttpResponse> MockList() =>
        [
            Mock(1, 1),
            Mock(2, 1) with { Title = "Записка - ред. 2", Status = "verified", SchemaCode = "01.06" },
            Mock(3, 2) with { ProjectName = "Реконструкция корпуса", Status = "has_errors" },
            Mock(4, 2) with { Status = "archived" },
        ];
}
