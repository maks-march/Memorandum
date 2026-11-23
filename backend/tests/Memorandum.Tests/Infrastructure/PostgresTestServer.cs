using Npgsql;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Сервер Postgres для тестов. Адрес берётся из <c>POSTGRES</c>, а если переменной нет —
/// из того же окружения и <c>infra/.env</c>, что читает dev-стек: тогда пароль совпадает
/// с тем, которым создан том базы.
/// </summary>
internal static class PostgresTestServer
{
    private static readonly Lazy<string> Resolved = new(BuildAdminConnectionString);

    public static string AdminConnectionString => Resolved.Value;

    /// <summary>
    /// Строка подключения к служебной базе <c>postgres</c>: по ней создаются базы тестов.
    /// <para>
    /// Пароль берётся из того же источника, что и у сервиса <c>db</c>: переменная окружения,
    /// затем <c>infra/.env</c> (его же читает compose), затем умолчания compose. Иначе тесты
    /// стучатся паролем по умолчанию в базу, созданную с паролем из <c>.env</c>, и получают 28P01.
    /// </para>
    /// </summary>
    private static string BuildAdminConnectionString()
    {
        var overridden = Environment.GetEnvironmentVariable("POSTGRES");

        if (!string.IsNullOrWhiteSpace(overridden))
            return overridden;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost",
            Port = int.TryParse(Environment.GetEnvironmentVariable("POSTGRES_PORT"), out var port) ? port : 5432,
            Database = "postgres",
            Username = Environment.GetEnvironmentVariable("POSTGRES_USER")
                       ?? InfraEnvFileValue("POSTGRES_USER") ?? "memorandum",
            Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                       ?? InfraEnvFileValue("POSTGRES_PASSWORD") ?? "memorandum",
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Значение из <c>infra/.env</c>. Файл ищется от каталога сборки вверх по дереву,
    /// поэтому тесты находят его и из IDE, и из командной строки.
    /// </summary>
    private static string? InfraEnvFileValue(string key)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "infra", ".env");

            if (File.Exists(candidate))
            {
                foreach (var line in File.ReadLines(candidate))
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith('#'))
                        continue;

                    if (trimmed.StartsWith(key + "=", StringComparison.Ordinal))
                        return trimmed[(key.Length + 1)..].Trim().Trim('"');
                }

                return null;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Одна внятная ошибка вместо «connection refused» или 28P01 в каждом тесте:
    /// что поднять и каким пользователем пробовали подключиться (пароль замаскирован).
    /// </summary>
    public static InvalidOperationException Unavailable(Exception exception) =>
        new(
            $"Postgres недоступен или не пускает ({Redact(AdminConnectionString)}). " +
            "Поднять рядом: `docker compose -f infra/docker-compose.dev.yml up -d db`. " +
            "Пароль берётся из POSTGRES, затем из POSTGRES_PASSWORD, затем из infra/.env, " +
            "затем умолчание compose — пароль должен совпадать с тем, которым создан том базы. " +
            exception.Message,
            exception);

    private static string Redact(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Password = "***" }.ConnectionString;
}
