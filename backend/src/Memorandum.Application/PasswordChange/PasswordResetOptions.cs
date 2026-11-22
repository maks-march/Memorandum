namespace Memorandum.Application.PasswordChange;

/// <summary>Настройки сброса пароля (<c>PasswordReset:*</c>).</summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int MaxAttempts { get; set; } = 3;

    public TimeSpan CodeTimeToLive { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Секрет для HMAC-хеша кода. В Production обязателен.</summary>
    public string CodePepper { get; set; } = string.Empty;
}
