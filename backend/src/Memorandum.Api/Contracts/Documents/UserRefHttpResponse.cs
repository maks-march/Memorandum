namespace Memorandum.Api.Contracts.Documents;

/// <summary>Ссылка на пользователя: автор, ответственный, подписант.</summary>
/// <param name="Id">Идентификатор пользователя.</param>
/// <param name="FullName">ФИО.</param>
public sealed record UserRefHttpResponse(long Id, string FullName)
{
    /// <summary>Моковая ссылка на пользователя.</summary>
    public static UserRefHttpResponse Mock(long id = 1) =>
        new(id, "Иванов Иван Иванович");
}
