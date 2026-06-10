namespace NajaEcho.Application.Features.Auth.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId);

public sealed record CurrentUserDto(Guid Id, string DisplayName, string? AvatarRef);
