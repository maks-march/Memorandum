namespace Memorandum.Domain.Documents;

/// <summary>Статус документа: черновик, результат проверки, архив.</summary>
public enum DocumentStatus
{
    /// <summary>Заполняется; единственный статус, в котором документ правится содержимым.</summary>
    Draft = 1,

    /// <summary>Последняя проверка прошла: XML можно выгрузить.</summary>
    Verified = 2,

    /// <summary>Последняя проверка нашла ошибки: выгрузка запрещена.</summary>
    HasErrors = 3,

    /// <summary>Архив: только чтение. Восстановление возвращает прежний статус.</summary>
    Archived = 4,
}

/// <summary>Разделы пояснительной записки: коды совпадают с <c>section</c> в API и манифесте схемы.</summary>
public enum DocumentSectionCode
{
    /// <summary>Сведения об объекте: наименование, адрес, ОКТМО, признаки.</summary>
    Object = 1,

    /// <summary>Участники: застройщик, технический заказчик, проектная организация.</summary>
    Participants = 2,

    /// <summary>Ответственные специалисты и подписанты.</summary>
    Specialists = 3,

    /// <summary>Основания для разработки документации.</summary>
    Basis = 4,

    /// <summary>Технико-экономические показатели.</summary>
    Tep = 5,

    /// <summary>Состав проектной документации.</summary>
    Composition = 6,

    /// <summary>Нормативные и исходно-разрешительные документы.</summary>
    Normatives = 7,

    /// <summary>Отчётная документация по инженерным изысканиям.</summary>
    Surveys = 8,

    /// <summary>Климатическая и инженерно-геологическая характеристика.</summary>
    Climate = 9,

    /// <summary>Прочие сведения по схеме.</summary>
    Extra = 10,
}

/// <summary>Коды разделов в виде строк для API: <c>object</c>, <c>tep</c>, <c>composition</c>.</summary>
public static class DocumentSections
{
    private static readonly Dictionary<DocumentSectionCode, string> Codes = new()
    {
        [DocumentSectionCode.Object] = "object",
        [DocumentSectionCode.Participants] = "participants",
        [DocumentSectionCode.Specialists] = "specialists",
        [DocumentSectionCode.Basis] = "basis",
        [DocumentSectionCode.Tep] = "tep",
        [DocumentSectionCode.Composition] = "composition",
        [DocumentSectionCode.Normatives] = "normatives",
        [DocumentSectionCode.Surveys] = "surveys",
        [DocumentSectionCode.Climate] = "climate",
        [DocumentSectionCode.Extra] = "extra",
    };

    public static IReadOnlyCollection<string> All => Codes.Values;

    public static string ToCode(DocumentSectionCode section) => Codes[section];

    /// <summary>Разбор кода из запроса: неизвестный раздел — ошибка валидации.</summary>
    public static DocumentSectionCode Parse(string? code)
    {
        var normalized = Common.Guard.Required(code, "Раздел документа", 50);

        var pair = Codes.FirstOrDefault(item =>
            string.Equals(item.Value, normalized, StringComparison.OrdinalIgnoreCase));

        return pair.Key == 0
            ? throw new Common.ValidationException(
                Common.ErrorCodes.SectionUnknown,
                $"Неизвестный раздел «{normalized}»: допустимые — {string.Join(", ", All)}.")
            : pair.Key;
    }
}
