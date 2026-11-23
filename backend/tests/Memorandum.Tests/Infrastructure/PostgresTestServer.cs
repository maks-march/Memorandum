using Npgsql;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Сервер Postgres для тестов. Адрес берётся из <c>POSTGRES</c>, по умолчанию —
/// локальный экземпляр из dev-стека (<c>infra/docker-compose.dev.yml</c>).
/// </summary>
internal static class PostgresTestServer
{
    public static string AdminConnectionString =>
        Environment.GetEnvironmentVariable("POSTGRES")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=memorandum;Password=memorandum";

    /// <summary>
    /// Проверка до первого теста: без живого Postgres сообщение должно говорить,
    /// что поднять, а не «connection refused» из недр Npgsql.
    /// </summary>
    public static async Task EnsureAvailableAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString);
            await connection.OpenAsync();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Postgres недоступен (" + Redact(AdminConnectionString) + "): тестам нужна живая БД. " +
                "Поднять рядом: `docker compose -f infra/docker-compose.dev.yml up -d db`. " +
                exception.Message,
                exception);
        }
    }

    private static string Redact(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Password = "***" }.ConnectionString;
}
