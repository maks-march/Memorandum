using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Memorandum.Api;

namespace Memorandum.Tests;

/// <summary>
/// Версия сборки — это git sha в <see cref="AssemblyInformationalVersionAttribute"/>,
/// а не <see cref="AssemblyName.Version"/>. По ней smoke после деплоя подтверждает,
/// что на стенде именно та сборка, которую везли.
/// </summary>
public sealed class BuildVersionTests(MemorandumApiFactory factory) : IClassFixture<MemorandumApiFactory>
{
    [Fact]
    public void Version_comes_from_informational_version()
    {
        var informational = typeof(BuildInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        Assert.False(string.IsNullOrWhiteSpace(informational));
        Assert.Equal(informational, BuildInfo.Version);
    }

    [Fact]
    public void Version_is_not_assembly_version()
    {
        var assemblyVersion = typeof(BuildInfo).Assembly.GetName().Version?.ToString();

        Assert.NotEqual(assemblyVersion, BuildInfo.Version);
    }

    [Fact]
    public async Task Health_and_system_info_expose_the_same_build_version()
    {
        var client = factory.CreateClient();

        var health = await client.GetFromJsonAsync<JsonElement>("/api/health");
        var info = await client.GetFromJsonAsync<JsonElement>("/api/v1/system/info");

        Assert.Equal(BuildInfo.Version, health.GetProperty("version").GetString());
        Assert.Equal(BuildInfo.Version, info.GetProperty("appVersion").GetString());
    }
}
