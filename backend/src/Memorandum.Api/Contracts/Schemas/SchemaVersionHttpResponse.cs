
namespace Memorandum.Api.Contracts.Schemas;

/// <summary>Версия XML-схемы: источник выбора схемы при создании документа.</summary>
/// <param name="Code">Код версии, например «01.07».</param>
/// <param name="Title">Название версии.</param>
/// <param name="EffectiveFrom">С какой даты действует.</param>
/// <param name="IsDefault">Версия по умолчанию: подпись «актуальная».</param>
public sealed record SchemaVersionHttpResponse(
    string Code,
    string Title,
    DateTime EffectiveFrom,
    bool IsDefault)
{
    /// <summary>Моковые версии для <c>/api/v1/mocks/schema-versions</c>.</summary>
    public static IReadOnlyList<SchemaVersionHttpResponse> MockList() =>
        [
            new("01.07", "Редакция 01.07", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsDefault: true),
            new("01.06", "Редакция 01.06", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsDefault: false),
        ];
}
