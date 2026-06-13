using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Hangar.GetOwningMembers;

public sealed class GetOwningMembersHandler(IHangarRepository repository)
{
    public Task<IReadOnlyList<OwningMember>> HandleAsync(GetOwningMembersQuery query, CancellationToken ct) =>
        repository.GetOwningMembersAsync(ct);
}
