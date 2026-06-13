namespace NajaEcho.Application.Features.Hangar.GetMyHangar;

public sealed record GetMyHangarQuery(
    Guid UserId,
    string? Search,
    int Page,
    int PageSize);
