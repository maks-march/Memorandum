using Memorandum.Domain.Common;

namespace Memorandum.Domain.Projects;

/// <summary>
/// Участие сотрудника в проекте. Часть агрегата <see cref="Project"/>:
/// создаётся и меняется только через него. Сотрудник задаётся ссылкой на
/// пользователя; его принадлежность организации проверяется при добавлении.
/// </summary>
public sealed class ProjectMember : AuditableEntity
{
    private ProjectMember()
    {
    }

    internal ProjectMember(long userId, DateTime utcNow, string? createdBy)
    {
        UserId = Guard.PositiveId(userId, "Сотрудник");
        IsActive = true;
        MarkCreated(utcNow, createdBy);
    }

    public long ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public long UserId { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Повторное добавление ранее выведенного из проекта сотрудника.</summary>
    internal void Reactivate(DateTime utcNow, string? updatedBy)
    {
        IsActive = true;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Вывод из проекта: запись остаётся в истории неактивной.</summary>
    internal void Deactivate(DateTime utcNow, string? updatedBy)
    {
        IsActive = false;
        Touch(utcNow, updatedBy);
    }
}
