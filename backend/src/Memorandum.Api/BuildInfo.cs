using System.Reflection;

namespace Memorandum.Api;

/// <summary>Сведения о сборке сервиса.</summary>
public static class BuildInfo
{
    /// <summary>
    /// Версия сборки в формате <c>1.0.0+abc1234</c>.
    /// Источник — <see cref="AssemblyInformationalVersionAttribute"/>: MSBuild дописывает в него
    /// <c>SourceRevisionId</c> (короткий git sha), который передаётся при сборке образа.
    /// <see cref="AssemblyName.Version"/> для этой задачи не годится — там всегда <c>1.0.0.0</c>,
    /// то есть по ответу health нельзя отличить одну сборку от другой.
    /// </summary>
    public static string Version { get; } = Resolve();

    private static string Resolve()
    {
        var assembly = typeof(BuildInfo).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return string.IsNullOrWhiteSpace(informational)
            ? assembly.GetName().Version?.ToString() ?? "1.0.0"
            : informational;
    }
}
