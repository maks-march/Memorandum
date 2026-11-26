
namespace Memorandum.Api.Contracts.Projects;

/// <summary>Счётчики документов в карточке проекта.</summary>
/// <param name="Total">Всего пояснительных записок.</param>
/// <param name="Draft">Черновиков.</param>
/// <param name="Verified">Проверенных.</param>
/// <param name="HasErrors">С ошибками.</param>
/// <param name="Archived">В архиве.</param>
public sealed record ProjectDocumentsCountHttpResponse(
    int Total,
    int Draft,
    int Verified,
    int HasErrors,
    int Archived)
{
    /// <summary>Моковые счётчики для карточки проекта.</summary>
    public static ProjectDocumentsCountHttpResponse Mock() => new(Total: 3, Draft: 1, Verified: 1, HasErrors: 0, Archived: 1);
}
