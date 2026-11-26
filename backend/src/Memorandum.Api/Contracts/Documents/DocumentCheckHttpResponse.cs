
namespace Memorandum.Api.Contracts.Documents;

/// <summary>Результат проверки документа.</summary>
/// <param name="Ok">Проверка прошла без ошибок.</param>
/// <param name="CheckedAt">Когда проверяли.</param>
/// <param name="Errors">Замечания проверки.</param>
/// <param name="Counts">Сколько ошибок и предупреждений.</param>
public sealed record DocumentCheckHttpResponse(
    bool Ok,
    DateTime CheckedAt,
    IReadOnlyList<DocumentCheckIssueHttpResponse> Errors,
    DocumentCheckCountsHttpResponse Counts)
{
    /// <summary>Моковый результат для <c>/api/v1/mocks/documents/{id}/checks</c>.</summary>
    public static DocumentCheckHttpResponse Mock() =>
        new(
            Ok: false,
            new DateTime(2026, 9, 4, 15, 0, 0, DateTimeKind.Utc),
            [
                new("object-info.area", "object-info", "REQUIRED", "Error", "Не заполнена площадь объекта", "/Документ/Объект/Площадь"),
                new("tep.rows", "tep", "TABLE_EMPTY", "Warning", "Таблица ТЭП пуста", "/Документ/ТЭП"),
            ],
            new(Errors: 1, Warnings: 1));

    /// <summary>Моковая история проверок.</summary>
    public static IReadOnlyList<DocumentCheckHttpResponse> MockList() => [Mock()];
}

/// <summary>Замечание проверки.</summary>
/// <param name="FieldId">Поле, к которому относится замечание.</param>
/// <param name="Section">Код раздела.</param>
/// <param name="Code">Код правила.</param>
/// <param name="Severity">Error или Warning.</param>
/// <param name="Message">Текст для пользователя.</param>
/// <param name="XmlPath">Путь в XML, куда попадает значение.</param>
public sealed record DocumentCheckIssueHttpResponse(
    string FieldId,
    string Section,
    string Code,
    string Severity,
    string Message,
    string? XmlPath);

/// <summary>Счётчики замечаний.</summary>
/// <param name="Errors">Ошибок.</param>
/// <param name="Warnings">Предупреждений.</param>
public sealed record DocumentCheckCountsHttpResponse(int Errors, int Warnings);
