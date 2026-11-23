using Memorandum.Domain.Common;

namespace Memorandum.Domain.Projects;

/// <summary>
/// Раздел пояснительной записки: «Сведения об объекте», «ТЭП», «Состав документации» и т. д.
/// Разделы задаются кодом, а не свободным текстом: по нему документ раскладывается в XML.
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

    public long DocumentId { get; private set; }

    public ProjectDocument Document { get; private set; } = null!;

    /// <summary>Код раздела по схеме (например «object-info», «tep», «composition»).</summary>
    public string Code { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public int SortOrder { get; private set; }

    /// <summary>Содержимое раздела. Структура полей определяется схемой, поэтому хранится как есть.</summary>
    public string? DataJson { get; private set; }

    public static DocumentSection Create(
        ProjectDocument document,
        string? code,
        string? title,
        int sortOrder,
        DateTime utcNow,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.IsArchived)
            throw new ConflictException(ErrorCodes.DocumentNotEditable, "Нельзя изменить архивный документ.");

        var section = new DocumentSection
        {
            Document = document,
            DocumentId = document.Id,
            Code = Guard.Required(code, "Код раздела", CodeMaxLength),
            Title = Guard.Required(title, "Название раздела", TitleMaxLength),
            SortOrder = GuardSortOrder(sortOrder),
        };

        section.MarkCreated(utcNow, createdBy);
        return section;
    }

    /// <summary>Заполнение раздела. Пустое значение очищает содержимое.</summary>
    public void Fill(string? dataJson, DateTime utcNow, string? updatedBy = null)
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
