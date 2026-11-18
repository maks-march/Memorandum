namespace Memorandum.Api.Contracts;

/// <summary>
/// Анти-CSRF токен. Токен приходит и в теле, и в cookie <c>XSRF-TOKEN</c>;
/// SPA отправляет его в заголовке <c>X-XSRF-TOKEN</c>.
/// </summary>
public sealed record CsrfHttpResponse(string XsrfToken);
