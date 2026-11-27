namespace Memorandum.Api.Contracts.Documents;

/// <summary>Собранный XML: хеш и размер, по которым видно, что ушло в экспертизу.</summary>
/// <param name="Id">Идентификатор артефакта.</param>
/// <param name="CheckId">Проверка, по которой собран XML.</param>
/// <param name="FileName">Имя файла для скачивания.</param>
/// <param name="Sha256">Хеш содержимого.</param>
/// <param name="SizeBytes">Размер файла.</param>
/// <param name="GeneratedAt">Когда собран.</param>
public sealed record DocumentXmlArtifactHttpResponse(
    long Id,
    long CheckId,
    string FileName,
    string Sha256,
    long SizeBytes,
    DateTime GeneratedAt)
{
    /// <summary>Моковый артефакт для <c>/api/v1/mocks/documents/{id}/xml</c>.</summary>
    public static DocumentXmlArtifactHttpResponse Mock(long id = 1) =>
        new(
            id,
            CheckId: 1,
            "Пояснительная записка-01.07.xml",
            "9f2c1d3b4a5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8",
            SizeBytes: 48213,
            new DateTime(2026, 9, 4, 15, 2, 0, DateTimeKind.Utc));

    /// <summary>Моковый список артефактов.</summary>
    public static IReadOnlyList<DocumentXmlArtifactHttpResponse> MockList() => [Mock(1)];
}
