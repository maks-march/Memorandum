namespace Memorandum.Api.Contracts;

/// <summary>Тело <c>POST /api/v1/auth/register</c>. Поля необязательные по типу: пустоту ловит валидатор.</summary>
/// <param name="Email">Email пользователя.</param>
/// <param name="Password">Пароль в открытом виде.</param>
/// <param name="FullName">ФИО.</param>
/// <param name="OrgName">Название организации; если задано, пользователь станет её администратором.</param>
public sealed record RegisterHttpRequest(string? Email, string? Password, string? FullName, string? OrgName);
