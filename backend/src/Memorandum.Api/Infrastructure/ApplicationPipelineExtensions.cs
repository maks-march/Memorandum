using Microsoft.AspNetCore.HttpOverrides;
using Memorandum.Api.Infrastructure.Errors;

namespace Memorandum.Api.Infrastructure;

/// <summary>
/// Настройка конвейера: middleware в правильном порядке + прокси-заголовки.
/// Регистрация сервисов — в <see cref="ServiceCollectionExtensions"/>.
/// </summary>
internal static class ApplicationPipelineExtensions
{
    /// <summary>
    /// Порядок важен: forward-заголовки до всего (нужен реальный IP для лимитов),
    /// лимиты — после роутинга и до авторизации, антифоржери — после аутентификации.
    /// </summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseRouting();

        app.UseRateLimiter();

        // До антифоржери: токен из cookie перекладывается в заголовок,
        // чтобы клиенту не приходилось делать это самому.
        app.UseAntiforgeryCookie();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.UseApiExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }

    /// <summary>
    /// Доверяем <c>X-Forwarded-*</c> только сетям nginx: иначе клиент подделает
    /// IP и обойдёт rate limiting. Список пуст — берём docker-подсеть по умолчанию.
    /// </summary>
    public static void AddTrustedProxies(this WebApplicationBuilder builder)
    {
        var trustedProxies = builder.Configuration.GetSection("Proxy:TrustedNetworks").Get<string[]>();
        if (trustedProxies is null or [])
        {
            if (builder.Environment.IsProduction())
                throw new InvalidOperationException(
                    "Production требует Proxy:TrustedNetworks — CIDR docker-сети с nginx " +
                    "(переменная Proxy__TrustedNetworks__0). Без него X-Forwarded-* отбрасываются.");

            trustedProxies = ["172.16.0.0/12"];
        }

        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
            foreach (var cidr in trustedProxies)
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
        });
    }
}
