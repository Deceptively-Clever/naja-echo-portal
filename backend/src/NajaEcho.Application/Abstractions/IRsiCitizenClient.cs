namespace NajaEcho.Application.Abstractions;

public sealed record RsiCitizenPage(string Content, string? DisplayName);
public sealed record RsiProfileNotFound;
public sealed record RsiUnreachable;

public interface IRsiCitizenClient
{
    Task<object> FetchCitizenAsync(string handle, CancellationToken ct = default);
}
