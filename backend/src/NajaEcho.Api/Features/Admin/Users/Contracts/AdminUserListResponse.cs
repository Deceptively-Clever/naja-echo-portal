namespace NajaEcho.Api.Features.Admin.Users.Contracts;

public sealed record AdminUserCharacterResponse(Guid Id, string Name, string Handle);

public sealed record AdminUserResponse(
    Guid Id,
    string AuthName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<AdminUserCharacterResponse> Characters);

public sealed record AdminUserListResponse(IReadOnlyList<AdminUserResponse> Users);
