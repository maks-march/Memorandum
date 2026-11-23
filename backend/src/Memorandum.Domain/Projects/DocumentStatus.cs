namespace Memorandum.Domain.Projects;

/// <summary>
/// Статус документа. Сейчас реализован путь «черновик → архив»:
/// проверка и выгрузка XML появятся вместе с обработкой схем.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Заполняется; единственный статус, в котором документ можно править.</summary>
    Draft = 1,

    /// <summary>Архивная редакция: только чтение.</summary>
    Archived = 2,
}
