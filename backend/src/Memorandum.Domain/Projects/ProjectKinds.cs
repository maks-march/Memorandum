using Memorandum.Domain.Common;

namespace Memorandum.Domain.Projects;

/// <summary>Вид работ по объекту: определяет набор и обязательность полей XML.</summary>
public enum WorkType
{
    /// <summary>Строительство.</summary>
    Construction = 1,

    /// <summary>Реконструкция.</summary>
    Reconstruction = 2,

    /// <summary>Капитальный ремонт.</summary>
    MajorRepair = 3,

    /// <summary>Снос.</summary>
    Demolition = 4,
}

/// <summary>Тип объекта капитального строительства: от него зависит каталог ТЭП.</summary>
public enum ObjectKind
{
    /// <summary>Линейный объект.</summary>
    Linear = 1,

    /// <summary>Производственный объект.</summary>
    Industrial = 2,

    /// <summary>Непроизводственный объект.</summary>
    NonIndustrial = 3,
}

/// <summary>Тип стороны в XML: организация, ИП, физлицо или иностранная структура.</summary>
public enum PartyType
{
    /// <summary>Организация сервиса или контрагент-организация.</summary>
    Organization = 1,

    /// <summary>Индивидуальный предприниматель.</summary>
    Entrepreneur = 2,

    /// <summary>Физическое лицо.</summary>
    Individual = 3,

    /// <summary>Иностранное юридическое лицо или структура без образования ЮЛ.</summary>
    Foreign = 4,
}

/// <summary>Допустимые значения полей проекта в виде строк — для API и импорта.</summary>
public static class ProjectKinds
{
    public static readonly IReadOnlyCollection<string> WorkTypes =
        Enum.GetNames<WorkType>();

    public static readonly IReadOnlyCollection<string> ObjectKinds =
        Enum.GetNames<ObjectKind>();

    public static readonly IReadOnlyCollection<string> PartyTypes =
        Enum.GetNames<PartyType>();

    public static ValidationException InvalidWorkType() =>
        new(ErrorCodes.Validation, $"Вид работ: допустимые значения — {string.Join(", ", WorkTypes)}.");

    public static ValidationException InvalidObjectKind() =>
        new(ErrorCodes.Validation, $"Тип объекта: допустимые значения — {string.Join(", ", ObjectKinds)}.");
}
