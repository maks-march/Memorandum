using System.Text.Json;
using System.Text.Json.Nodes;

namespace Memorandum.Domain.Documents;

/// <summary>Содержимое документа как JSON по разделам.</summary>
/// <remarks>
/// Хранится одной колонкой <c>jsonb</c>: структуру полей задаёт манифест версии схемы,
/// поэтому БД не знает про конкретные поля. Ключ верхнего уровня — код раздела.
/// </remarks>
public static class DocumentData
{
    /// <summary>Кириллица пишется как есть: содержимое читают люди и диффят по нему.</summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Содержимое раздела; <c>null</c>, если раздел не заполнялся.</summary>
    public static string? Section(string? dataJson, DocumentSectionCode section)
    {
        var root = Parse(dataJson);

        return root[DocumentSections.ToCode(section)] is { } node
            ? node.ToJsonString(Options)
            : null;
    }

    /// <summary>Заменяет содержимое раздела, не трогая остальные.</summary>
    public static string WithSection(string? dataJson, DocumentSectionCode section, string? sectionJson)
    {
        var root = Parse(dataJson);
        var code = DocumentSections.ToCode(section);

        if (string.IsNullOrWhiteSpace(sectionJson))
        {
            root.Remove(code);
            return root.ToJsonString(Options);
        }

        try
        {
            root[code] = JsonNode.Parse(sectionJson);
        }
        catch (JsonException exception)
        {
            throw new Common.ValidationException(
                Common.ErrorCodes.InvalidFormat,
                $"Содержимое раздела «{code}» должно быть корректным JSON: {exception.Message}");
        }

        return root.ToJsonString(Options);
    }

    /// <summary>Заполненные разделы: по ним считается прогресс.</summary>
    public static IReadOnlyCollection<DocumentSectionCode> FilledSections(string? dataJson)
    {
        var root = Parse(dataJson);

        return Enum.GetValues<DocumentSectionCode>()
            .Where(section => root[DocumentSections.ToCode(section)] is not null)
            .ToArray();
    }

    private static JsonObject Parse(string? dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
            return [];

        try
        {
            return JsonNode.Parse(dataJson) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
