using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Hangar.RemoveShipFromHangar;

public sealed class RemoveShipFromHangarHandler(IHangarRepository repository)
{
    public Task HandleAsync(RemoveShipFromHangarCommand command, CancellationToken ct) =>
        repository.RemoveAsync(command.UserId, command.ShipId, ct);
}
