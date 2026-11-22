namespace Memorandum.Api.Infrastructure;

/// <summary>
/// Проверки конфигурации на старте окружения. Смысл — падать сразу и с понятным
/// текстом, а не через сутки на первом запросе (fail-fast вместо «а почему 500?»).
/// </summary>
internal static class BuilderExtensions
{
    /// <summary>
    /// Что недопустимо в Production: SQLite, пустая строка подключения, отсутствие
    /// соли кодов, включённые заглушки и AllowedHosts со звёздочкой.
    /// </summary>
    public static void ProductionGuards(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsProduction())
            return;

        if (!string.Equals(builder.Configuration["Database:Provider"], "Postgres", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Production требует Database:Provider = Postgres. Проверьте appsettings.Production.json.");

        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Postgres")))
            throw new InvalidOperationException(
                "Production требует ConnectionStrings:Postgres (переменная окружения ConnectionStrings__Postgres).");

        if (string.IsNullOrWhiteSpace(builder.Configuration["PasswordReset:CodePepper"]))
            throw new InvalidOperationException(
                "Production требует PasswordReset:CodePepper — иначе хеш кода подбирается из дампа Redis.");

        if (builder.Configuration.GetValue("Mock:Enabled", false))
            throw new InvalidOperationException(
                "Production не может работать с Mock:Enabled=true. Заглушки должны быть отключены.");

        var allowedHosts = builder.Configuration["AllowedHosts"];

        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Contains('*'))
            throw new InvalidOperationException(
                "Production требует конкретный AllowedHosts (домен через ';'), значение '*' запрещено. " +
                "Задайте переменную окружения AllowedHosts.");
    }
}
