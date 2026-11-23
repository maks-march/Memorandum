using Memorandum.Domain.Common;

namespace Memorandum.Domain.Projects;

/// <summary>
/// Пояснительная записка в рамках проекта: основная сущность сервиса.
/// Под одним проектом их может быть несколько — по версиям и редакциям.
/// </summary>
public sealed class ProjectDocument : AuditableEntity
{
    public const int TitleMaxLength = 500;
    public const int SchemaCodeMaxLength = 20;

    private ProjectDocument()
    {
    }

    public long ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    /// <summary>
    /// Код XML-схемы Минстроя, по которой заполняется документ (например «01.07»).
    /// Задаётся при создании: смена схемы — это другой документ, а не правка.
    /// </summary>
    public string SchemaCode { get; private set; } = null!;

    /// <summary>Черновик — единственный рабочий статус; архив — только чтение.</summary>
    public DocumentStatus Status { get; private set; }

    public bool IsArchived { get; private set; }

    public static ProjectDocument Create(
        Project project,
        string? title,
        string? schemaCode,
        DateTime utcNow,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (project.IsArchived)
            throw new ConflictException(
                ErrorCodes.ProjectArchived,
                "Нельзя создать документ в архивном проекте.");

        var document = new ProjectDocument
        {
            Project = project,
            ProjectId = project.Id,
            Title = Guard.Required(title, "Заголовок документа", TitleMaxLength),
            SchemaCode = Guard.Required(schemaCode, "Код схемы", SchemaCodeMaxLength),
            Status = DocumentStatus.Draft,
        };

        document.MarkCreated(utcNow, createdBy);
        return document;
    }

    public void Rename(string? title, DateTime utcNow, string? updatedBy = null)
    {
        EnsureEditable("Нельзя изменить архивный документ.");

        Title = Guard.Required(title, "Заголовок документа", TitleMaxLength);
        Touch(utcNow, updatedBy);
    }

    public void Archive(DateTime utcNow, string? updatedBy = null)
    {
        if (IsArchived)
            return;

        IsArchived = true;
        Status = DocumentStatus.Archived;
        Touch(utcNow, updatedBy);
    }

    public void Restore(DateTime utcNow, string? updatedBy = null)
    {
        if (!IsArchived)
            return;

        IsArchived = false;
        Status = DocumentStatus.Draft;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Черновик — единственный статус, в котором документ правится.</summary>
    private void EnsureEditable(string message)
    {
        if (IsArchived || Status != DocumentStatus.Draft)
            throw new ConflictException(ErrorCodes.DocumentNotEditable, message);
    }
}
