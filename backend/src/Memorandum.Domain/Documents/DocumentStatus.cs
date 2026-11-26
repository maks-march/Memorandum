namespace Memorandum.Domain.Documents;

/// <summary>
/// Статус документа: черновик, результат последней проверки, архив.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Заполняется; единственный статус, в котором документ можно править содержимым.</summary>
    Draft = 1,

    /// <summary>Последняя проверка прошла: XML можно выгрузить.</summary>
    Verified = 2,

    /// <summary>Последняя проверка нашла ошибки: выгрузка запрещена.</summary>
    HasErrors = 3,

    /// <summary>Архивная редакция: только чтение.</summary>
    Archived = 4,
}
