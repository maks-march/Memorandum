namespace Memorandum.Api.Contracts.Admin;

/// <summary>Ошибка приложения для <c>/api/v1/admin/errors</c>.</summary>
/// <param name="Id">Идентификатор ошибки.</param>
/// <param name="Level">Warning, Error или Fatal.</param>
/// <param name="Code">Код ошибки: код домена или INFRASTRUCTURE.</param>
/// <param name="Message">Текст ошибки.</param>
/// <param name="Path">Путь запроса.</param>
/// <param name="TraceId">TraceId для поиска в логах.</param>
/// <param name="RepeatCount">Сколько раз повторилась.</param>
/// <param name="LastOccurredAt">Когда повторилась последний раз.</param>
public sealed record AppErrorHttpResponse(
    long Id,
    string Level,
    string Code,
    string Message,
    string? Path,
    string? TraceId,
    int RepeatCount,
    DateTime LastOccurredAt)
{
    /// <summary>Моковая ошибка.</summary>
    public static AppErrorHttpResponse Mock(long id = 1) =>
        new(
            id,
            "Error",
            "XML_GENERATION_FAILED",
            "Не удалось собрать XML: документ не прошёл проверку",
            "/api/v1/documents/1/xml",
            "0HN7A1B2C3D4E:00000002",
            RepeatCount: 3,
            new DateTime(2026, 9, 4, 15, 5, 0, DateTimeKind.Utc));

    /// <summary>Моковый список ошибок.</summary>
    public static IReadOnlyList<AppErrorHttpResponse> MockList() =>
        [
            Mock(1),
            Mock(2) with { Level = "Warning", Code = "STORAGE_UNAVAILABLE", RepeatCount = 1 },
        ];
}
