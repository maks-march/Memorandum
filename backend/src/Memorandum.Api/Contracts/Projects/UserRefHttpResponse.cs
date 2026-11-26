
namespace Memorandum.Api.Contracts.Projects;

/// <summary>Ссылка на сотрудника: ответственный проекта или автор документа.</summary>
/// <param name="Id">Идентификатор пользователя.</param>
/// <param name="FullName">ФИО как показывается в списках.</param>
public sealed record UserRefHttpResponse(long Id, string FullName)
{
    /// <summary>Моковая версия для заглушек.</summary>
    public static UserRefHttpResponse Mock(long id = 2, string fullName = "Петров Иван Сергеевич") =>
        new(id, fullName);
}
