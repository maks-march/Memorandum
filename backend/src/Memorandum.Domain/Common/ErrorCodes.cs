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

    // организации
    public const string OrganizationInactive = "ORGANIZATION_INACTIVE";
    public const string MemberAlreadyExists = "MEMBER_ALREADY_EXISTS";
    public const string LastOrgAdminRequired = "LAST_ORG_ADMIN_REQUIRED";

    // бизнес
    public const string ReferenceInUse = "REFERENCE_IN_USE";
}
