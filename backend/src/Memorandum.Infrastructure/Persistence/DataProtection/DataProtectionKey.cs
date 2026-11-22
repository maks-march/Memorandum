using System.Xml.Linq;
using Memorandum.Domain.Common;

namespace Memorandum.Infrastructure.Persistence.DataProtection;

/// <summary>
/// Ключ Data Protection в БД. Нужен, чтобы cookie и анти-CSRF токены переживали
/// перезапуск контейнера и работали на нескольких репликах: ключи не хранятся на диске.
/// </summary>
public sealed class DataProtectionKey : AuditableEntity
{
    public const int FriendlyNameMaxLength = 100;

    private DataProtectionKey()
    {
    }

    /// <summary>Имя ключа, которое ведёт Data Protection.</summary>
    public string FriendlyName { get; private set; } = null!;

    /// <summary>Сериализованный ключ (XML).</summary>
    public string Xml { get; private set; } = null!;

    /// <summary>Ключ создаётся хранилищем Data Protection, аудит ведёт БД.</summary>
    public static DataProtectionKey Create(string friendlyName, XElement xml) =>
        new()
        {
            FriendlyName = Guard.Required(friendlyName, "Имя ключа", FriendlyNameMaxLength),
            Xml = Guard.Required(xml.ToString(SaveOptions.DisableFormatting), "Ключ"),
        };

    public XElement ToXml() => XElement.Parse(Xml);
}
