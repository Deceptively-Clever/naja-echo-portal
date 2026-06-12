namespace NajaEcho.Application.Abstractions;

public sealed record LocalUser(Guid Id, string DisplayName, string DiscordUsername);
