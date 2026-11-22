namespace Memorandum.Domain.Organizations;

/// <summary>Роль пользователя внутри организации. Глобального администратора сервиса здесь нет.</summary>
public enum OrganizationRole
{
    /// <summary>Управляет организацией, участниками и их ролями.</summary>
    OrgAdmin = 1,

    /// <summary>Работает с проектами и документами организации.</summary>
    Editor = 2,

    /// <summary>Только чтение.</summary>
    Viewer = 3,
}
