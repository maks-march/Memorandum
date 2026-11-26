
namespace Memorandum.Api.Contracts.Documents;

/// <summary>Импорт документа из XML.</summary>
/// <param name="Id">Идентификатор импорта.</param>
/// <param name="FileName">Имя загруженного файла.</param>
/// <param name="DetectedSchemaCode">Распознанная версия схемы.</param>
/// <param name="ImportedFields">Сколько полей перенесено.</param>
/// <param name="Warnings">Поля, которые не удалось перенести.</param>
/// <param name="CreatedAt">Когда выполнен импорт.</param>
public sealed record DocumentImportHttpResponse(
    long Id,
    string FileName,
    string? DetectedSchemaCode,
    int ImportedFields,
    IReadOnlyList<ImportWarningHttpResponse> Warnings,
    DateTime CreatedAt)
{
    /// <summary>Моковый импорт для <c>/api/v1/mocks/documents/{id}/imports</c>.</summary>
    public static DocumentImportHttpResponse Mock(long id = 1) =>
        new(
            id,
            "zapiska07.xml",
            "01.07",
            ImportedFields: 118,
            [new("СведенияОбОбъекте/КадастровыйНомер", "Поле не найдено в схеме документа")],
            new DateTime(2026, 9, 4, 11, 0, 0, DateTimeKind.Utc));

    /// <summary>Моковый список импортов.</summary>
    public static IReadOnlyList<DocumentImportHttpResponse> MockList() => [Mock(1)];
}

/// <summary>Предупреждение импорта: что и почему не перенесено.</summary>
/// <param name="Path">Путь в XML.</param>
/// <param name="Reason">Причина.</param>
public sealed record ImportWarningHttpResponse(string Path, string Reason);
