
namespace Memorandum.Api.Contracts.Projects;

/// <summary>Проект в списке и в карточке: <c>/api/v1/projects</c>.</summary>
/// <param name="Id">Идентификатор проекта.</param>
/// <param name="Name">Наименование проекта.</param>
/// <param name="ObjectName">Наименование объекта капитального строительства.</param>
/// <param name="AddressText">Местоположение объекта.</param>
/// <param name="Code">Шифр проекта в организации.</param>
/// <param name="ResponsibleUserId">Ответственный сотрудник.</param>
/// <param name="IsArchived">Проект в архиве: только чтение.</param>
/// <param name="DocumentsCount">Сколько пояснительных записок в проекте.</param>
/// <param name="UpdatedAt">Момент последнего изменения.</param>
public sealed record ProjectHttpResponse(
    long Id,
    string Name,
    string ObjectName,
    string? AddressText,
    string? Code,
    long? ResponsibleUserId,
    bool IsArchived,
    int DocumentsCount,
    DateTime UpdatedAt)
{
    /// <summary>Моковая версия для <c>/api/v1/mocks/projects</c>: данные не из БД.</summary>
    public static ProjectHttpResponse Mock(long id = 1) =>
        new(
            id,
            "Жилой комплекс № 1",
            "Жилой дом по ул. Ленина, 15",
            "г. Екатеринбург, ул. Ленина, 15",
            "1234-ПЗ",
            ResponsibleUserId: 2,
            IsArchived: false,
            DocumentsCount: 2,
            UpdatedAt: new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));

    /// <summary>Моковый список: два проекта, один в архиве.</summary>
    public static IReadOnlyList<ProjectHttpResponse> MockList() =>
        [Mock(1), Mock(2) with { Name = "Реконструкция корпуса", IsArchived = true, DocumentsCount = 0 }];
}
