namespace Memorandum.Application.Invitations;

/// <summary>Настройки кодов приглашения (<c>Invitations:*</c>).</summary>
public sealed class InvitationOptions
{
    public const string SectionName = "Invitations";

    /// <summary>Срок жизни кода: сутки. Повторная выдача сдвигает срок.</summary>
    public TimeSpan CodeTimeToLive { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Секрет для HMAC-хеша кода: в хранилище открытый код не попадает.</summary>
    public string CodePepper { get; set; } = string.Empty;
}
