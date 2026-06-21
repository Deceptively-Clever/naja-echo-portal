namespace NajaEcho.Application.Features.Admin.Users.GetUsers;

public sealed record AdminUserDto(
    Guid Id,
    string AuthName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<AdminUserCharacterDto> Characters);
