using Memorandum.Domain.Common;

namespace Memorandum.Domain.Documents;

/// <summary>Результат проверки документа: когда, по какой схеме, с какими замечаниями.</summary>
/// <remarks>История проверок append-only: правка содержимого не удаляет прежние результаты.</remarks>
public sealed class DocumentCheck : AuditableEntity
{
    private DocumentCheck()
    {
    }

    internal DocumentCheck(
        Document document,
        long schemaVersionId,
        bool ok,
        IReadOnlyCollection<DocumentCheckIssue> issues,
        DateTime utcNow,
        string? createdBy)
    {
        Document = document;
        DocumentId = document.Id;
        SchemaVersionId = schemaVersionId;
        Ok = ok;
        IssuesJson = DocumentCheckIssues.Serialize(issues);
        ErrorCount = issues.Count(issue => issue.IsError);
        WarningCount = issues.Count(issue => !issue.IsError);
        CheckedAt = utcNow;

        // Проверка без ошибок не может нести ошибки: расхождение — ошибка вызывающего кода.
        if (ok && ErrorCount > 0)
            throw new ValidationException(
                ErrorCodes.ValidationFailed,
                "Проверка с ошибками не может считаться успешной.");

        MarkCreated(utcNow, createdBy);
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    /// <summary>Версия схемы, по которой выполнялась проверка.</summary>
    public long SchemaVersionId { get; private set; }

    public Schemas.SchemaVersion? SchemaVersion { get; private set; }

    public DateTime CheckedAt { get; private set; }

    /// <summary>Итог: <c>true</c> — ошибок нет, XML можно выгружать.</summary>
    public bool Ok { get; private set; }

    public int ErrorCount { get; private set; }

    public int WarningCount { get; private set; }

    public string IssuesJson { get; private set; } = "[]";

    public IReadOnlyCollection<DocumentCheckIssue> Issues => DocumentCheckIssues.Parse(IssuesJson);
}
