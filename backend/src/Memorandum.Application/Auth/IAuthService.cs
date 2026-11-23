using Memorandum.Application.Common;

namespace Memorandum.Application.Auth;

/// <summary>Сценарии аутентификации.</summary>
public interface IAuthService
{
    /// <summary>Регистрация. Вход не выполняет: cookie выдаёт отдельный сценарий.</summary>
    Task<Result<AuthUserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Проверка учётных данных. Ошибка не различает «нет пользователя», «неверный пароль» и «заблокирован».</summary>
    /// <summary>
    /// Регистрация сотрудника по коду приглашения: организация и роль берутся из кода.
    /// Бесплатная регистрация (<see cref="RegisterAsync"/>) больше организацию не создаёт.
    /// </summary>
    Task<Result<AuthUserDto>> RegisterByInviteAsync(
        RegisterRequest request,
        string? code,
        CancellationToken cancellationToken = default);

    Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
