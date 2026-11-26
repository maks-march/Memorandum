using Memorandum.Domain.Common;

namespace Memorandum.Domain.Documents;

/// <summary>
/// Раздел пояснительной записки: «Сведения об объекте», «ТЭП», «Состав документации» и т. д.
/// Часть агрегата <see cref="Document"/>: создаётся и заполняется только через него.
/// </summary>
public sealed class DocumentSection : AuditableEntity
{
    public const int CodeMaxLength = 50;
    public const int TitleMaxLength = 200;

    /// <summary>Порядок разделов в документе — тот же, что в схеме.</summary>
    public const int MaxSortOrder = 1000;

    private DocumentSection()
    {
    }

    internal DocumentSection(
        Document document,
        string code,
        string? title,
        int sortOrder,
        DateTime utcNow,
        string? createdBy = null)
    {
        Document = document;
        DocumentId = document.Id;
        Code = code;
        Title = Guard.Required(title, "Название раздела", TitleMaxLength);
        SortOrder = GuardSortOrder(sortOrder);

        MarkCreated(utcNow, createdBy);
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    /// <summary>Код раздела по схеме (например «object-info», «tep», «composition»).</summary>
    public string Code { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public int SortOrder { get; private set; }

    /// <summary>Содержимое раздела. Структура полей определяется схемой, поэтому хранится как есть.</summary>
    public string? DataJson { get; private set; }

    /// <summary>Заполнение раздела. Пустое значение очищает содержимое.</summary>
    internal void Fill(string? dataJson, DateTime utcNow, string? updatedBy = null)
    {
        if (Document.IsArchived)
            throw new ConflictException(ErrorCodes.DocumentNotEditable, "Нельзя изменить архивный документ.");

        DataJson = string.IsNullOrWhiteSpace(dataJson) ? null : dataJson;
        Touch(utcNow, updatedBy);
    }

    internal void ChangeSortOrder(int sortOrder, DateTime utcNow, string? updatedBy)
    {
        SortOrder = GuardSortOrder(sortOrder);
        Touch(utcNow, updatedBy);
    }

    private static int GuardSortOrder(int sortOrder)
    {
        if (sortOrder is < 0 or > MaxSortOrder)
            throw new ValidationException(
                ErrorCodes.Validation,
                $"Порядок раздела должен быть от 0 до {MaxSortOrder}.");

        return sortOrder;
    }
}
