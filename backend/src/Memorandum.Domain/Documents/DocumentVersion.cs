using Memorandum.Domain.Common;

namespace Memorandum.Domain.Documents;

/// <summary>
/// Снапшот документа: содержимое всех разделов на момент сохранения.
/// Версии не правятся и не удаляются — история остаётся историей.
/// </summary>
public sealed class DocumentVersion : AuditableEntity
{
    public const int CommentMaxLength = 500;

    private DocumentVersion()
    {
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    /// <summary>Номер версии в документе: 1, 2, 3… Без пропусков.</summary>
    public int Number { get; private set; }

    /// <summary>Содержимое документа целиком, как оно было на момент снапшота.</summary>
    public string DataJson { get; private set; } = null!;

    /// <summary>Комментарий автора; у автоматических снапшотов его нет.</summary>
    public string? Comment { get; private set; }

    /// <summary>Автоматический снапшот (перед восстановлением, при проверке), а не ручной.</summary>
    public bool IsAuto { get; private set; }

    /// <summary>Статус документа на момент снапшота.</summary>
    public DocumentStatus Status { get; private set; }

    internal DocumentVersion(
        int number,
        string? dataJson,
        DocumentStatus status,
        DateTime utcNow,
        string? comment = null,
        bool isAuto = false,
        string? createdBy = null)
    {
        if (number <= 0)
            throw new ValidationException(ErrorCodes.Validation, "Номер версии должен быть больше нуля.");

        Number = number;
        DataJson = Guard.Required(dataJson, "Содержимое версии");
        Status = status;
        Comment = Guard.Optional(comment, "Комментарий", CommentMaxLength);
        IsAuto = isAuto;

        MarkCreated(utcNow, createdBy);
    }
}
