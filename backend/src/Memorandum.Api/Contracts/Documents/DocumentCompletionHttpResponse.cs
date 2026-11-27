namespace Memorandum.Api.Contracts.Documents;

/// <summary>Прогресс заполнения документа.</summary>
/// <param name="Percent">Общий процент по обязательным полям.</param>
/// <param name="Sections">Разбивка по разделам.</param>
public sealed record DocumentCompletionHttpResponse(
    int Percent,
    IReadOnlyList<DocumentSectionCompletionHttpResponse> Sections)
{
    /// <summary>Моковый прогресс для карточки документа.</summary>
    public static DocumentCompletionHttpResponse Mock() =>
        new(
            42,
            [
                new("object", Filled: 4, Total: 6, Required: 5),
                new("tep", Filled: 2, Total: 9, Required: 6),
                new("composition", Filled: 3, Total: 3, Required: 3),
            ]);
}

/// <summary>Прогресс по одному разделу.</summary>
/// <param name="Code">Код раздела.</param>
/// <param name="Filled">Сколько полей заполнено.</param>
/// <param name="Total">Сколько всего полей.</param>
/// <param name="Required">Сколько из них обязательные.</param>
public sealed record DocumentSectionCompletionHttpResponse(string Code, int Filled, int Total, int Required);
