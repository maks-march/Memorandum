namespace Memorandum.Application.Auth;

/// <summary>Запрос регистрации: <c>POST /api/v1/auth/register</c>.</summary>
/// <param name="Email">Email; уникальность проверяется по нормализованному значению.</param>
/// <param name="Password">Пароль в открытом виде — приходит по HTTPS и сразу хешируется.</param>
/// <param name="FullName">ФИО.</param>
/// <param name="OrgName">Название организации; если задано, пользователь становится её администратором.</param>
public sealed record RegisterRequest(string? Email, string? Password, string? FullName, string? OrgName);
