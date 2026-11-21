namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Тест, которому нужен живой Redis.
/// <para>
/// По умолчанию тест выполняется и, если Redis недоступен, падает с подсказкой —
/// так отсутствие инфраструктуры в CI не превращается в тихое «пропущено».
/// <c>SKIP_REDIS_TESTS=1</c> помечает такие тесты пропущенными: удобно для локального
/// прогона без Redis (xunit v2 не умеет пропускать тест из тела метода).
/// </para>
/// </summary>
public sealed class RedisFactAttribute : FactAttribute
{
    public const string SkipVariable = "SKIP_REDIS_TESTS";

    public RedisFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(SkipVariable) == "1")
            Skip = $"Тесты категории Redis отключены переменной {SkipVariable}=1.";
    }
}
