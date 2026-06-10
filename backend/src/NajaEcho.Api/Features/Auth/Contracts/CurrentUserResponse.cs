namespace NajaEcho.Api.Features.Auth.Contracts;

public sealed record CurrentUserResponse(Guid Id, string DisplayName, string? AvatarUrl);
