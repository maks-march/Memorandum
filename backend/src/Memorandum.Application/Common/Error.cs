using Memorandum.Domain.Common;

namespace Memorandum.Application.Common;

/// <summary>
/// Ошибка сценария. Без HTTP-статусов: код превращает в статус Api-слой.
/// </summary>
/// <param name="Code">Стабильный код из <see cref="ErrorCodes"/>.</param>
/// <param name="Message">Текст для пользователя.</param>
/// <param name="FieldErrors">Ошибки по полям формы — для <c>ValidationProblemDetails</c>.</param>
/// <param name="Details">Дополнительные данные для клиента.</param>
public sealed record Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null,
    IReadOnlyDictionary<string, object>? Details = null)
{
    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new(ErrorCodes.Validation, "Запрос содержит некорректные поля.", fieldErrors);

    public static Error NotFound(string entityName, object key) =>
        new(ErrorCodes.NotFound, $"{entityName} не найден.", Details: new Dictionary<string, object> { ["key"] = key });

    public static Error Forbidden(string message) => new(ErrorCodes.Forbidden, message);

    public static Error Business(string code, string message) => new(code, message);

    public static Error FromDomainException(DomainException exception) => new(exception.Code, exception.Message);

}
