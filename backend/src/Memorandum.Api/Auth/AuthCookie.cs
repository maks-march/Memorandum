namespace Memorandum.Api.Auth;

/// <summary>
/// Параметры auth-cookie в одном месте: имя зависит от окружения, флаги — от схемы запроса.
/// </summary>
internal static class AuthCookie
{
    /// <summary>В Production требуется префикс <c>__Host-</c>: браузер принимает такую cookie только от своего домена.</summary>
    public const string ProductionName = "__Host-memo.sid";

    public const string DevelopmentName = "memo.sid";

    public static string Name(IHostEnvironment environment) =>
        environment.IsProduction() ? ProductionName : DevelopmentName;
}
