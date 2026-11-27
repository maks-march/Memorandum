using Memorandum.Domain.Common;

namespace Memorandum.Domain.Documents;

/// <summary>Снимок содержимого документа: ручной или автоматический.</summary>
/// <remarks>
/// Версии append-only: не правятся и не удаляются. Номер уникален внутри документа
/// и присваивается агрегатом.
/// </remarks>
public sealed class DocumentVersion : AuditableEntity
{
    public const int CommentMaxLength = 300;

    private DocumentVersion()
    {
    }

    internal DocumentVersion(
        Document document,
        int number,
        string? dataJson,
        DocumentStatus status,
        DateTime utcNow,
        string? comment,
        bool isAuto,
        string? createdBy)
    {
        Document = document;
        DocumentId = document.Id;
        Number = number;
        DataJson = string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson;
        Status = status;
        Comment = Guard.Optional(comment, "Комментарий к версии", CommentMaxLength);
        IsAuto = isAuto;

        MarkCreated(utcNow, createdBy);
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    /// <summary>Порядковый номер внутри документа, начиная с 1.</summary>
    public int Number { get; private set; }

    /// <summary>Полный снимок содержимого: восстанавливать по разделам нельзя — они связаны.</summary>
    public string DataJson { get; private set; } = "{}";

    /// <summary>Статус документа на момент снимка.</summary>
    public DocumentStatus Status { get; private set; }

    public string? Comment { get; private set; }

    /// <summary>Автоматическая версия: создана перед восстановлением или импортом.</summary>
    public bool IsAuto { get; private set; }
}
