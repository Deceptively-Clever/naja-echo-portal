using System.Security.Cryptography;

namespace NajaEcho.Domain.Characters;

public sealed class PendingCharacterRegistration
{
    public static readonly TimeSpan ValidityWindow = TimeSpan.FromMinutes(30);

    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static PendingCharacterRegistration Create(Guid ownerUserId, DateTimeOffset now)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(16);
        var token = "naja-" + Convert.ToBase64String(tokenBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        return new PendingCharacterRegistration
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Token = token,
            ExpiresAt = now + ValidityWindow,
            CreatedAt = now,
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
