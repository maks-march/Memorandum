using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Memorandum.Tests;

/// <summary>
/// Проверяет fail-fast прод-конфигурации: приложение не должно стартовать
/// с dev-настройками. Это защита от «задеплоили не тот appsettings».
/// </summary>
public sealed class EnvironmentGuardsTests
{
    private static MemorandumApiFactory Production(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Postgres",
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=memorandum;Username=u;Password=p",
            ["AllowedHosts"] = "memorandum.example.ru",
            ["Proxy:TrustedNetworks:0"] = "172.28.0.0/16",
        };

        foreach (var (key, value) in overrides ?? [])
            settings[key] = value;

        return new MemorandumApiFactory { Environment = "Production", Settings = settings };
    }

    [Fact]
    public void Production_without_postgres_connection_string_fails_fast()
    {
        using var factory = Production(new() { ["ConnectionStrings:Postgres"] = "" });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("ConnectionStrings:Postgres", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Production_with_wildcard_allowed_hosts_fails_fast()
    {
        using var factory = Production(new() { ["AllowedHosts"] = "*" });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("AllowedHosts", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Production_without_trusted_proxy_networks_fails_fast()
    {
        using var factory = new MemorandumApiFactory
        {
            Environment = "Production",
            Settings = new()
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=memorandum;Username=u;Password=p",
                ["AllowedHosts"] = "memorandum.example.ru",
                },
        };

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Proxy:TrustedNetworks", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Development_always_has_mock_endpoints()
    {
        using var factory = new MemorandumApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/mock/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Production_has_no_mock_endpoints()
    {
        using var factory = Production(new());   // существующий хелпер
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/mock/api/me")).StatusCode);
    }
}
