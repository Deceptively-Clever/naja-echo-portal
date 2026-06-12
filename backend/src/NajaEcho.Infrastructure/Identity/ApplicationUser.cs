using Microsoft.AspNetCore.Identity;

namespace NajaEcho.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string DiscordUsername { get; set; } = string.Empty;
}
