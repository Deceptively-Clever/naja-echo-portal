namespace NajaEcho.Application.Features.Hangar.GetOrgHangar;

public sealed record GetOrgHangarQuery(
    Guid CurrentUserId,
    string? Search,
    bool Mine,
    Guid? MemberId,
    int Page,
    int PageSize,
    string SortBy = "ownerCount");
