using Memorandum.Application.Common;

namespace Memorandum.Application.PasswordChange;

/// <summary>Смена пароля по коду из письма.</summary>
public interface IPasswordChangeService
{
    /// <summary>Запрос кода. Ответ не зависит от того, есть ли такой email.</summary>
    Task<Result> RequestCodeAsync(
        Models.RequestPasswordChangeCodeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Подтверждение кода и установка нового пароля.</summary>
    Task<Result> ConfirmAsync(
        long userId,
        string? code,
        Models.ConfirmPasswordChangeRequest request,
        CancellationToken cancellationToken = default);
}
