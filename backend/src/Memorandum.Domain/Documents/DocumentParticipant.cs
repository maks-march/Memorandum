using Memorandum.Domain.Common;
using Memorandum.Domain.Projects;

namespace Memorandum.Domain.Documents;

/// <summary>Роль лица в документе.</summary>
public enum DocumentParticipantKind
{
    /// <summary>Застройщик или технический заказчик.</summary>
    Customer = 1,

    /// <summary>Проектная организация.</summary>
    Designer = 2,

    /// <summary>Главный инженер проекта: в документе один.</summary>
    Gip = 3,

    /// <summary>Главный архитектор проекта.</summary>
    Gap = 4,

    /// <summary>Нормативно-техническое сопровождение.</summary>
    Nrs = 5,

    /// <summary>Изыскательская организация.</summary>
    Surveyor = 6,

    /// <summary>Подрядчик.</summary>
    Contractor = 7,
}

/// <summary>Лицо, указанное в документе: организация, ИП, физлицо или иностранная структура.</summary>
/// <remarks>
/// Сущность принадлежит документу и не имеет собственного жизненного цикла:
/// добавляется и удаляется через <see cref="Document"/>. Должность — свойство участия
/// в документе, а не пользователя.
/// </remarks>
public sealed class DocumentParticipant : AuditableEntity
{
    public const int FullNameMaxLength = 300;
    public const int PositionMaxLength = 200;
    public const int SnilsMaxLength = 14;
    public const int NrsMaxLength = 12;

    private DocumentParticipant()
    {
    }

    internal DocumentParticipant(
        Document document,
        DocumentParticipantKind kind,
        PartyType partyType,
        string? fullName,
        string? position,
        string? snils,
        string? nrsNopriz,
        int order,
        long? organizationId,
        long? userId,
        DateTime utcNow,
        string? createdBy)
    {
        Document = document;
        DocumentId = document.Id;
        Kind = kind;
        PartyType = partyType;
        FullName = Guard.Required(fullName, "Наименование участника", FullNameMaxLength);
        Position = Guard.Optional(position, "Должность", PositionMaxLength);
        Snils = NormalizeSnils(snils);
        NrsNopriz = NormalizeNrs(nrsNopriz);
        Order = order;
        OrganizationId = organizationId;
        UserId = userId;

        MarkCreated(utcNow, createdBy);
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    public DocumentParticipantKind Kind { get; private set; }

    public PartyType PartyType { get; private set; }

    /// <summary>Наименование организации или ФИО физлица — так, как попадёт в XML.</summary>
    public string FullName { get; private set; } = null!;

    public string? Position { get; private set; }

    /// <summary>СНИЛС специалиста: обязателен для ГИП, ГАП и НРС.</summary>
    public string? Snils { get; private set; }

    /// <summary>Номер в НРС НОПРИЗ: у ГИП и ГАП проверяется формат.</summary>
    public string? NrsNopriz { get; private set; }

    /// <summary>Порядок вывода в документе и XML.</summary>
    public int Order { get; private set; }

    /// <summary>Ссылка на организацию сервиса, если участник — клиент системы.</summary>
    public long? OrganizationId { get; private set; }

    /// <summary>Ссылка на пользователя сервиса, если участник — сотрудник клиента.</summary>
    public long? UserId { get; private set; }

    public void Change(
        string? fullName,
        string? position,
        string? snils,
        string? nrsNopriz,
        DateTime utcNow,
        string? updatedBy = null)
    {
        // Закрываем XML-правило: у ГИП/ГАП/НРС формат НРС обязателен.
        FullName = Guard.Required(fullName, "Наименование участника", FullNameMaxLength);
        Position = Guard.Optional(position, "Должность", PositionMaxLength);
        Snils = NormalizeSnils(snils);
        NrsNopriz = NormalizeNrs(nrsNopriz);

        Touch(utcNow, updatedBy);
    }

    public void Reorder(int order, DateTime utcNow, string? updatedBy = null)
    {
        Order = order;
        Touch(utcNow, updatedBy);
    }

    private static string? NormalizeSnils(string? value)
    {
        var normalized = Guard.Optional(value, "СНИЛС", SnilsMaxLength);

        return normalized is null
            ? null
            : Guard.Matches(normalized, "СНИЛС", Guard.Snils(), "11 цифр");
    }

    private static string? NormalizeNrs(string? value)
    {
        var normalized = Guard.Optional(value, "НРС НОПРИЗ", NrsMaxLength);

        if (normalized is null)
            return null;

        // Часть специалистов НРС не требует: значение фиксируем как есть.
        if (string.Equals(normalized, "Не требуется", StringComparison.OrdinalIgnoreCase))
            return "Не требуется";

        return Guard.Matches(normalized, "НРС НОПРИЗ", Guard.NrsNopriz(), "П-123456, ПИ-123456 или И-123456");
    }
}
