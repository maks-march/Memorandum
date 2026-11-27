namespace Memorandum.Api.Contracts.Documents;

/// <summary>Версия документа: снапшот содержимого.</summary>
/// <param name="Number">Номер версии в документе.</param>
/// <param name="CreatedAt">Когда снят снапшот.</param>
/// <param name="CreatedBy">Кто снял снапшот.</param>
/// <param name="Comment">Комментарий автора.</param>
/// <param name="IsAuto">Автоматический снапшот, а не ручной.</param>
/// <param name="Status">Статус документа на момент снапшота.</param>
public sealed record DocumentVersionHttpResponse(
    int Number,
    DateTime CreatedAt,
    string? CreatedBy,
    string? Comment,
    bool IsAuto,
    string Status)
{
    /// <summary>Моковая версия для <c>/api/v1/mocks/documents/{id}/versions</c>.</summary>
    public static DocumentVersionHttpResponse Mock(int number = 1) =>
        new(
            number,
            new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc),
            "Иванов Иван Иванович",
            Comment: number == 1 ? "Первая редакция" : null,
            IsAuto: number > 1,
            "draft");

    /// <summary>Моковый список версий.</summary>
    public static IReadOnlyList<DocumentVersionHttpResponse> MockList() => [Mock(1), Mock(2)];
}
