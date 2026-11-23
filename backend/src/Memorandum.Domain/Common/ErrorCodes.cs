namespace Memorandum.Domain.Common;

/// <summary>
/// Коды ошибок — стабильная часть контракта: фронт маппит их на тексты и поля формы.
/// Новый код здесь = осознанное изменение API.
/// </summary>
public static class ErrorCodes
{
    // общие
    public const string Required = "REQUIRED";
    public const string TooLong = "TOO_LONG";
    public const string Validation = "VALIDATION";
    public const string NotFound = "NOT_FOUND";
    public const string Forbidden = "FORBIDDEN";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserInactive = "USER_INACTIVE";
    public const string EmailDuplicate = "EMAIL_DUPLICATE";
    public const string PasswordTooWeak = "PASSWORD_TOO_WEAK";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string AntiforgeryTokenInvalid = "ANTIFORGERY_TOKEN_INVALID";

    // организации
    public const string OrganizationInactive = "ORGANIZATION_INACTIVE";
    public const string MemberAlreadyExists = "MEMBER_ALREADY_EXISTS";
    public const string LastOrgAdminRequired = "LAST_ORG_ADMIN_REQUIRED";

    // проекты
    public const string ProjectArchived = "PROJECT_ARCHIVED";
    public const string ProjectMemberRequired = "PROJECT_MEMBER_REQUIRED";
    public const string OrganizationMismatch = "ORGANIZATION_MISMATCH";

    // приглашения
    public const string InviteCodeInvalid = "INVITE_CODE_INVALID";
    public const string InviteCodeExpired = "INVITE_CODE_EXPIRED";
    public const string InviteCodeMissing = "INVITE_CODE_MISSING";
    public const string InviteTooManyAttempts = "INVITE_TOO_MANY_ATTEMPTS";

    // смена пароля
    public const string ResetCodeInvalid = "RESET_CODE_INVALID";
    public const string ResetCodeExpired = "RESET_CODE_EXPIRED";
    public const string ResetTicketNotFound = "RESET_TICKET_NOT_FOUND";
    public const string ResetAttemptsExhausted = "RESET_ATTEMPTS_EXHAUSTED";
    public const string ResetTooManyRequests = "RESET_TOO_MANY_REQUESTS";

    // инфраструктура
    public const string StorageUnavailable = "STORAGE_UNAVAILABLE";

    // бизнес
    public const string ReferenceInUse = "REFERENCE_IN_USE";
}
