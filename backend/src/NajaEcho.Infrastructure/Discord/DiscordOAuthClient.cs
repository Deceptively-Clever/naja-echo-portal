using System.Net.Http.Json;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Users;

namespace NajaEcho.Infrastructure.Discord;

public sealed class DiscordOAuthClient(HttpClient http) : IDiscordOAuthClient
{
    public async Task<DiscordProfile> GetUserProfileAsync(string accessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<DiscordUserDto>(ct)
            ?? throw new InvalidOperationException("Discord returned an empty user profile.");

        return new DiscordProfile
        {
            Id = dto.Id,
            Username = dto.Username,
            GlobalName = dto.GlobalName,
            Avatar = dto.Avatar,
            Email = dto.Email,
            Verified = dto.Verified ?? false,
        };
    }

    private sealed record DiscordUserDto(
        string Id,
        string Username,
        string? GlobalName,
        string? Avatar,
        string? Email,
        bool? Verified);
}
