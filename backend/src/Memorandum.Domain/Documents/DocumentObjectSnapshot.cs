using Memorandum.Domain.Common;
using Memorandum.Domain.Projects;

namespace Memorandum.Domain.Documents;

/// <summary>
/// Снимок полей объекта на момент создания документа: объект, вид работ, адрес, ОКТМО.
/// </summary>
/// <remarks>
/// Копия, а не ссылка: правка карточки проекта не должна менять уже проверенный XML.
/// Собственными полями не является — хранится в той же таблице, что и документ.
/// </remarks>
public sealed class DocumentObjectSnapshot
{
    public const int NameMaxLength = 500;
    public const int AddressMaxLength = 500;
    public const int RegionCodeMaxLength = 2;
    public const int OktmoCodeMaxLength = 11;
    public const int OktmoNameMaxLength = 200;

    private DocumentObjectSnapshot()
    {
    }

    public string ObjectName { get; private set; } = null!;

    public WorkType WorkType { get; private set; }

    public ObjectKind ObjectKind { get; private set; }

    public string? AddressText { get; private set; }

    public string? RegionCode { get; private set; }

    public string? OktmoCode { get; private set; }

    public string? OktmoName { get; private set; }

    /// <summary>Копирование полей объекта из проекта.</summary>
    public static DocumentObjectSnapshot FromProject(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new DocumentObjectSnapshot
        {
            ObjectName = project.ObjectName,
            WorkType = project.WorkType,
            ObjectKind = project.ObjectKind,
            AddressText = project.AddressText,
            RegionCode = project.RegionCode,
            OktmoCode = project.OktmoCode,
            OktmoName = project.OktmoName,
        };
    }

    /// <summary>Ручная правка снимка: часть сведений об объекте уточняется в документе.</summary>
    public void Change(
        string? objectName,
        WorkType workType,
        ObjectKind objectKind,
        string? addressText,
        string? regionCode,
        string? oktmoCode,
        string? oktmoName)
    {
        ObjectName = Guard.Required(objectName, "Наименование объекта", NameMaxLength);
        WorkType = workType;
        ObjectKind = objectKind;
        AddressText = Guard.Optional(addressText, "Адрес объекта", AddressMaxLength);

        RegionCode = Projects.Project.NormalizeRegion(regionCode);
        OktmoCode = Projects.Project.NormalizeOktmo(oktmoCode);

        OktmoName = Guard.Optional(oktmoName, "Наименование ОКТМО", OktmoNameMaxLength);
    }
}
