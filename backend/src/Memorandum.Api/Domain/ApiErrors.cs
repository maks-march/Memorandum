namespace Memorandum.Api.Domain;

/// <summary>Ошибки домена. Code используется как HTTP-статус в <c>ProblemDetails</c>.</summary>
public static class ApiErrors
{
    public static readonly Error EmailRequired = Error.New(400, "Email обязателен");
    public static readonly Error InvalidEmail = Error.New(400, "Некорректный email");
    public static readonly Error FullNameRequired = Error.New(400, "ФИО обязательно");
    public static readonly Error OrganizationNameRequired = Error.New(400, "Название организации обязательно");
    public static readonly Error OrganizationInactive = Error.New(409, "Организация деактивирована");
    public static readonly Error LastOrgAdmin = Error.New(409, "Нельзя убрать последнего администратора организации");
    public static readonly Error MemberAlreadyExists = Error.New(409, $"Пользователь уже состоит в организации");
    public static readonly Error MemberNotFound = Error.New(404, $"Пользователь не состоит в организации");
}
