using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Memorandum.Application.Abstractions;

namespace Memorandum.Infrastructure.Identity;

/// <summary>Данные текущего запроса из <see cref="HttpContext"/>: id и имя для аудита.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public long? Id =>
        long.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public string? Name => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
}
