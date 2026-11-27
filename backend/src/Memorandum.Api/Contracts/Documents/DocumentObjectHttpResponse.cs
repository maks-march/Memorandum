namespace Memorandum.Api.Contracts.Documents;

/// <summary>Снимок объекта в документе: поля объекта на момент создания записки.</summary>
/// <param name="ObjectName">Наименование объекта капитального строительства.</param>
/// <param name="WorkType">Вид работ.</param>
/// <param name="ObjectKind">Тип объекта.</param>
/// <param name="AddressText">Местоположение объекта.</param>
/// <param name="RegionCode">Код субъекта РФ.</param>
/// <param name="OktmoCode">Код ОКТМО.</param>
/// <param name="OktmoName">Наименование ОКТМО.</param>
public sealed record DocumentObjectHttpResponse(
    string ObjectName,
    string WorkType,
    string ObjectKind,
    string? AddressText,
    string? RegionCode,
    string? OktmoCode,
    string? OktmoName)
{
    /// <summary>Моковые сведения об объекте.</summary>
    public static DocumentObjectHttpResponse Mock() =>
        new(
            "Многоквартирный жилой дом",
            "Construction",
            "NonIndustrial",
            "г. Екатеринбург, ул. Ленина, 15",
            "66",
            "65701000",
            "город Екатеринбург");
}
