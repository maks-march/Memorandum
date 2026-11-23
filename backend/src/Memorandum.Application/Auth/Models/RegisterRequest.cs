namespace Memorandum.Application.Auth;


public interface IRegisterRequest
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? FullName { get; init; }
}

/// <summary>Запрос регистрации: <c>POST /api/v1/auth/register</c>.</summary>
/// <param name="Email">Email; уникальность проверяется по нормализованному значению.</param>
/// <param name="Password">Пароль в открытом виде — приходит по HTTPS и сразу хешируется.</param>
/// <param name="FullName">ФИО.</param>
/// <param name="OrgName">Название организации; если задано, пользователь становится её администратором.</param>
public sealed record RegisterRequest(string? Email, string? Password, string? FullName, string? OrgName) : IRegisterRequest;

/// <summary>Запрос регистрации: <c>POST /api/v1/auth/register</c>.</summary>
/// <param name="Email">Email; уникальность проверяется по нормализованному значению.</param>
/// <param name="Password">Пароль в открытом виде — приходит по HTTPS и сразу хешируется.</param>
/// <param name="FullName">ФИО.</param>
/// <param name="InviteCode">Код-приглашение для вступления в существующую организацию.</param>
public sealed record RegisterWithInviteRequest(string? Email, string? Password, string? FullName, string? InviteCode) : IRegisterRequest;
