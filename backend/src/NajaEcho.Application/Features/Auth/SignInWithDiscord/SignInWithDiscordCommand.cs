using NajaEcho.Domain.Users;

namespace NajaEcho.Application.Features.Auth.SignInWithDiscord;

public sealed record SignInWithDiscordCommand(DiscordProfile Profile);

public sealed record SignInWithDiscordResult(Guid UserId, string DisplayName, string? AvatarRef);
