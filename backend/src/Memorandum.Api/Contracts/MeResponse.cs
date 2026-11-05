
namespace Memorandum.Api.Contracts;

/// <summary>Профиль: организация приходит объектом, а не голым id.</summary>
public sealed record MeResponse(
    Guid Id,
    string Email,
    string FullName,
    string Position,
    string Role,
    OrgRef? Org);

public record OrgRef(Guid Id, string Name);