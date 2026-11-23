namespace Memorandum.Domain.Organizations;

/// <summary>Состояние приглашения сотрудника.</summary>
public enum InvitationStatus
{
    /// <summary>Ожидает принятия или отзыва.</summary>
    Pending = 1,

    /// <summary>Принято: пользователь стал участником организации.</summary>
    Accepted = 2,

    /// <summary>Отозвано администратором.</summary>
    Revoked = 3,
}
