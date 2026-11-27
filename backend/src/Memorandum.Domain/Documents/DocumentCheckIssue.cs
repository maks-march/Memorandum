using System.Text.Json;

namespace Memorandum.Domain.Documents;

/// <summary>Уровень замечания проверки.</summary>
public enum DocumentIssueSeverity
{
    /// <summary>Ошибка: XML не выгружается.</summary>
    Error = 1,

    /// <summary>Предупреждение: выгрузка возможна, но замечание сохраняется.</summary>
    Warning = 2,
}

/// <summary>Замечание проверки: где и что не так.</summary>
public sealed class DocumentCheckIssue
{
    private DocumentCheckIssue()
    {
    }

    public DocumentCheckIssue(
        DocumentIssueSeverity severity,
        string? code,
        string? message,
        DocumentSectionCode? section = null,
        string? field = null)
    {
        Severity = severity;
        Code = Common.Guard.Required(code, "Код замечания", 50);
        Message = Common.Guard.Required(message, "Текст замечания", 500);
        Section = section;
        Field = Common.Guard.Optional(field, "Поле", 100);
    }

    public DocumentIssueSeverity Severity { get; private set; }

    /// <summary>Код правила проверки: по нему фронт подсвечивает поле.</summary>
    public string Code { get; private set; } = null!;

    public string Message { get; private set; } = null!;

    public DocumentSectionCode? Section { get; private set; }

    /// <summary>Путь к полю внутри раздела, например <c>items[0].power</c>.</summary>
    public string? Field { get; private set; }

    public bool IsError => Severity == DocumentIssueSeverity.Error;
}

/// <summary>Разбор и сериализация списка замечаний в JSON-массив.</summary>
public static class DocumentCheckIssues
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Ошибки и предупреждения одним списком: замечания всегда приходят из одной проверки.</summary>
    public static IReadOnlyCollection<DocumentCheckIssue> Parse(string? issuesJson)
    {
        if (string.IsNullOrWhiteSpace(issuesJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<DocumentCheckIssue[]>(issuesJson, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Serialize(IReadOnlyCollection<DocumentCheckIssue> issues) =>
        JsonSerializer.Serialize(issues, Options);
}
