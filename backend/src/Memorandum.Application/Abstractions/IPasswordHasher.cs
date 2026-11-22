namespace Memorandum.Application.Abstractions;

public enum PasswordVerificationResult
{
    Failed,
    Success,

    /// <summary>Пароль верный, но хеш устарел по параметрам — перехешировать при входе.</summary>
    SuccessRehashNeeded,
}

/// <summary>Хеширование пароля. Алгоритм выбирает Infrastructure, домен и Application его не знают.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationResult Verify(string passwordHash, string password);
}
