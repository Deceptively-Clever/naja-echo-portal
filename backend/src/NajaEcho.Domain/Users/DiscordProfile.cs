namespace NajaEcho.Domain.Users;

public sealed class DiscordProfile
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? GlobalName { get; init; }
    public string? Avatar { get; init; }
    public string? Email { get; init; }
    public bool Verified { get; init; }

    public string DisplayName => GlobalName ?? Username;

    public string? AdmissibleEmail => Verified && !string.IsNullOrWhiteSpace(Email) ? Email : null;
}
