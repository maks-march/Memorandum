namespace Memorandum.Tests;

/// <summary>
/// Детерминированные часы: сценарий получает из них ровно тот момент, который задал тест.
/// </summary>
public sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
