namespace Memorandum.Api.Contracts.Documents;

/// <summary>Карточка документа: <c>/api/v1/documents/{id}</c>.</summary>
/// <param name="Document">Метаданные документа.</param>
/// <param name="Object">Снимок сведений об объекте.</param>
/// <param name="Responsible">Ответственный за документ.</param>
/// <param name="Sections">Разделы редактора.</param>
/// <param name="Completion">Прогресс заполнения.</param>
public sealed record DocumentDetailsHttpResponse(
    DocumentHttpResponse Document,
    DocumentObjectHttpResponse Object,
    UserRefHttpResponse? Responsible,
    IReadOnlyList<DocumentSectionHttpResponse> Sections,
    DocumentCompletionHttpResponse Completion)
{
    /// <summary>Моковая карточка для <c>/api/v1/mocks/documents/{id}</c>.</summary>
    public static DocumentDetailsHttpResponse Mock(long id = 1) =>
        new(
            DocumentHttpResponse.Mock(id),
            DocumentObjectHttpResponse.Mock(),
            UserRefHttpResponse.Mock(),
            DocumentSectionHttpResponse.MockList(),
            DocumentCompletionHttpResponse.Mock());
}
