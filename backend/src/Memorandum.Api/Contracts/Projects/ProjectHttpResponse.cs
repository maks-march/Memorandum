
namespace Memorandum.Api.Contracts.Projects;

/// <summary>Проект в списке и в карточке: <c>/api/v1/projects</c>.</summary>
/// <param name="Id">Идентификатор проекта.</param>
/// <param name="Name">Наименование проекта.</param>
/// <param name="ObjectName">Наименование объекта капитального строительства.</param>
/// <param name="WorkType">Вид работ: Construction, Reconstruction, MajorRepair или Demolition.</param>
/// <param name="ObjectKind">Тип объекта: Linear, Industrial или NonIndustrial.</param>
/// <param name="RegionCode">Код субъекта РФ.</param>
/// <param name="OktmoCode">Код ОКТМО: обязателен в XML с версии 01.06.</param>
/// <param name="AddressText">Местоположение объекта.</param>
/// <param name="Code">Шифр проекта в организации.</param>
/// <param name="Responsible">Ответственный сотрудник.</param>
/// <param name="IsArchived">Проект в архиве: только чтение.</param>
/// <param name="DocumentsCount">Сколько пояснительных записок в проекте.</param>
/// <param name="CreatedAt">Момент создания.</param>
public sealed record ProjectHttpResponse(
    long Id,
    string Name,
    string ObjectName,
    string WorkType,
    string ObjectKind,
    string? AddressText,
    string? RegionCode,
    string? OktmoCode,
    string? Code,
    UserRefHttpResponse? Responsible,
    bool IsArchived,
    int DocumentsCount,
    DateTime CreatedAt)
{
    /// <summary>Моковая версия для <c>/api/v1/mocks/projects</c>.</summary>
    public static ProjectHttpResponse Mock(long id = 1) =>
        new(
            id,
            "Жилой комплекс № 1",
            "Многоквартирный жилой дом",
            "Construction",
            "NonIndustrial",
            "г. Екатеринбург, ул. Ленина, 15",
            "66",
            "65701000",
            "1234-ПЗ",
            UserRefHttpResponse.Mock(),
            IsArchived: false,
            DocumentsCount: 3,
            CreatedAt: new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc));

    /// <summary>Моковый список: два проекта, один в архиве.</summary>
    public static IReadOnlyList<ProjectHttpResponse> MockList() =>
        [
            Mock(1),
            Mock(2) with
            {
                Name = "Реконструкция корпуса",
                ObjectName = "Корпус № 2",
                WorkType = "Reconstruction",
                ObjectKind = "Linear",
                OktmoCode = "65701000",
                IsArchived = true,
                DocumentsCount = 1,
            },
        ];
}
