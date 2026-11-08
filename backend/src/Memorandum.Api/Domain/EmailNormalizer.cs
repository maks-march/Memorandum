namespace Memorandum.Api.Domain;

/// <summary>Приведение email к виду, пригодному для сравнения и уникального индекса.</summary>
public static class EmailNormalizer
{
    public static string Normalize(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        return raw.Trim().ToLowerInvariant();
    }
}
