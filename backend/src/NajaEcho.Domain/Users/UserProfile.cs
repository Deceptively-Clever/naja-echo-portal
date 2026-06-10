namespace NajaEcho.Domain.Users;

public sealed class UserProfile
{
    public Guid Id { get; private set; }
    public string DiscordUserId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarRef { get; private set; }
    public string? Email { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset LastLoginAtUtc { get; private set; }
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }

    private UserProfile() { }

    public static UserProfile CreateFromDiscord(DiscordProfile profile, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.Id);
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            DiscordUserId = profile.Id,
            DisplayName = profile.DisplayName,
            AvatarRef = profile.Avatar,
            Email = profile.AdmissibleEmail,
            CreatedAtUtc = now,
            LastLoginAtUtc = now,
            LastUpdatedAtUtc = now,
        };
    }

    public void RecordLogin(DiscordProfile current, DateTimeOffset now)
    {
        LastLoginAtUtc = now;

        bool profileChanged = false;

        if (DisplayName != current.DisplayName)
        {
            DisplayName = current.DisplayName;
            profileChanged = true;
        }

        if (AvatarRef != current.Avatar)
        {
            AvatarRef = current.Avatar;
            profileChanged = true;
        }

        string? newEmail = current.AdmissibleEmail;
        if (Email != newEmail)
        {
            Email = newEmail;
            profileChanged = true;
        }

        if (profileChanged)
            LastUpdatedAtUtc = now;
    }
}
