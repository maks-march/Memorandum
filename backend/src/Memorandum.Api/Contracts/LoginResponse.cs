using Memorandum.Api.Controllers;

namespace Memorandum.Api.Contracts;

/// <summary>Ответ входа: кроме cookie отдаём xsrf-токен, фронт шлёт его в X-XSRF-TOKEN.</summary>
public sealed record LoginResponse(MeResponse User, string XsrfToken);
