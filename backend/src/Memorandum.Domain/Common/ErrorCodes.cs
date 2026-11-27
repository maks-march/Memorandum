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
    public const string InvalidFormat = "INVALID_FORMAT";
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
    public const string LastServiceAdminRequired = "LAST_SERVICE_ADMIN_REQUIRED";

    // проекты
    public const string ProjectArchived = "PROJECT_ARCHIVED";
    public const string ProjectMemberRequired = "PROJECT_MEMBER_REQUIRED";
    public const string OrganizationMismatch = "ORGANIZATION_MISMATCH";
    public const string ProjectHasDocuments = "PROJECT_HAS_DOCUMENTS";

    // документы
    public const string DocumentArchived = "DOCUMENT_ARCHIVED";
    public const string DocumentNotEditable = "DOCUMENT_NOT_EDITABLE";
    public const string DocumentNotVerified = "DOCUMENT_NOT_VERIFIED";
    public const string DocumentSchemaImmutable = "DOCUMENT_SCHEMA_IMMUTABLE";
    public const string DocumentTitleDuplicate = "DOCUMENT_TITLE_DUPLICATE";
    public const string SectionDuplicate = "SECTION_DUPLICATE";
    public const string SectionUnknown = "SECTION_UNKNOWN";
    public const string SectionRequired = "SECTION_REQUIRED";
    public const string SchemaMismatch = "SCHEMA_MISMATCH";
    public const string SchemaVersionInactive = "SCHEMA_VERSION_INACTIVE";

    // проверка и выгрузка XML
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string XmlGenerationFailed = "XML_GENERATION_FAILED";

    // версии схем
    public const string SchemaNotFound = "SCHEMA_NOT_FOUND";
    public const string SchemaInactive = "SCHEMA_INACTIVE";
    public const string SchemaCompileFailed = "SCHEMA_COMPILE_FAILED";
    public const string SchemaInUse = "SCHEMA_IN_USE";

    // импорт
    public const string ImportUnknownVersion = "IMPORT_UNKNOWN_VERSION";
    public const string ImportParseFailed = "IMPORT_PARSE_FAILED";
    public const string ImportPartial = "IMPORT_PARTIAL";

    // участники документа
    public const string ParticipantDuplicateGip = "PARTICIPANT_DUPLICATE_GIP";
    public const string ParticipantNrsFormat = "PARTICIPANT_NRS_FORMAT";
    public const string ParticipantPartyRequired = "PARTICIPANT_PARTY_REQUIRED";

    // конкурентность
    public const string StaleVersion = "STALE_VERSION";
    public const string ForceRequired = "FORCE_REQUIRED";

    // приглашения
    public const string InvitationExpired = "INVITATION_EXPIRED";
    public const string InvitationAlreadyHandled = "INVITATION_ALREADY_HANDLED";

    // приглашения по коду
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
