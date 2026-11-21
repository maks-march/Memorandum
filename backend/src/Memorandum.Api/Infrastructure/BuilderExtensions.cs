
namespace Memorandum.Api.Infrastructure;

public static class BuilderExtensions 
{
    public static void ProductionGuards(this WebApplicationBuilder builder, IWebHostEnvironment env, string provider, string? allowedHosts)
    {
        if (env.IsProduction())
        {
            if (!string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
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

            if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Contains('*'))
                throw new InvalidOperationException(
                    "Production требует конкретный AllowedHosts (домен через ';'), значение '*' запрещено. " +
                    "Задайте переменную окружения AllowedHosts.");
        }
    }
}
