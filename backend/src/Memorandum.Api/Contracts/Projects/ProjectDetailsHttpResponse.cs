
namespace Memorandum.Api.Contracts.Projects;

/// <summary>Карточка проекта: сам проект и счётчики документов.</summary>
/// <param name="Project">Данные проекта.</param>
/// <param name="Documents">Сколько документов и в каких статусах.</param>
public sealed record ProjectDetailsHttpResponse(
    ProjectHttpResponse Project,
    ProjectDocumentsCountHttpResponse Documents)
{
    /// <summary>Моковая карточка для <c>/api/v1/mocks/projects/{id}</c>.</summary>
    public static ProjectDetailsHttpResponse Mock(long id = 1) =>
        new(ProjectHttpResponse.Mock(id), ProjectDocumentsCountHttpResponse.Mock());
}
