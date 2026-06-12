using FluentAssertions;
using NajaEcho.Domain.Users;
using Xunit;

namespace NajaEcho.Domain.Tests.Users;

public class DiscordProfileTests
{
    [Fact]
    public void DisplayName_PrefersGlobalName()
    {
        var p = new DiscordProfile { Id = "1", Username = "user", GlobalName = "Global" };
        p.DisplayName.Should().Be("Global");
    }

    [Fact]
    public void DisplayName_FallsBackToUsername()
    {
        var p = new DiscordProfile { Id = "1", Username = "user", GlobalName = null };
        p.DisplayName.Should().Be("user");
    }
}
