using Memorandum.Application.Common;

namespace Memorandum.Application.Auth;

/// <summary>Сценарии аутентификации.</summary>
public interface IAuthService
{
    /// <summary>Регистрация. Вход не выполняет: cookie выдаёт отдельный сценарий.</summary>
    Task<Result<AuthUserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Проверка учётных данных. Ошибка не различает «нет пользователя», «неверный пароль» и «заблокирован».</summary>
    Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
