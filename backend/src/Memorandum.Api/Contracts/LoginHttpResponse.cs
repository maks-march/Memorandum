namespace Memorandum.Api.Contracts;

/// <summary>Ответ на вход: профиль и актуальный анти-CSRF токен (после входа cookie сменилась).</summary>
public sealed record LoginHttpResponse(MeHttpResponse User, string XsrfToken);
