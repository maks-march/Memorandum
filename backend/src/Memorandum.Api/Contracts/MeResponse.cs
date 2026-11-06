
namespace Memorandum.Api.Contracts;

/// <summary>Профиль: организация приходит объектом, а не голым id.</summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Position,
    string Role,
    Guid? OrgId);

public record OrgRef(Guid Id, string Name);