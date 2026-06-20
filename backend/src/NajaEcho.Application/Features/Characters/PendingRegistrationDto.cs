namespace NajaEcho.Application.Features.Characters;

public sealed record PendingRegistrationDto(string Token, DateTimeOffset ExpiresAt);
