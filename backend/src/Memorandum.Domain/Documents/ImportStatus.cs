namespace Memorandum.Domain.Documents;

/// <summary>Результат импорта XML.</summary>
public enum ImportStatus
{
    /// <summary>Все поля перенесены.</summary>
    Success = 1,

    /// <summary>Перенесена часть полей: список потерь во <see cref="DocumentImport.WarningsJson"/>.</summary>
    Partial = 2,

    /// <summary>Файл не разобран или версия схемы неизвестна: содержимое не изменено.</summary>
    Failed = 3,
}
