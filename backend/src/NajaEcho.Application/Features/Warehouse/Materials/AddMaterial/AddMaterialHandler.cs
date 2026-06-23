using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;

namespace NajaEcho.Application.Features.Warehouse.Materials.AddMaterial;

public sealed class AddMaterialHandler(
    IMaterialInventoryRepository repository,
    ICommodityRepository commodityRepository,
    IUserRepository userRepository,
    ILogger<AddMaterialHandler> logger)
{
    public async Task<(MaterialRowDto Row, bool IsNew)> HandleAsync(AddMaterialCommand command, CancellationToken ct)
    {
        var location = command.Location.Trim();

        if (string.IsNullOrEmpty(location))
        {
            throw new ArgumentException("Location must not be empty.", nameof(command));
        }

        var quantity = Math.Round(command.Quantity, 3, MidpointRounding.AwayFromZero);
        if (quantity <= 0.000m)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Quantity must be greater than 0.000.");
        }

        if (command.Quality < 1 || command.Quality > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Quality must be between 1 and 1000.");
        }

        var commodity = await commodityRepository.GetByIdAsync(command.CommodityId, ct);
        if (commodity is null || commodity.Status != Domain.Commodities.CommodityStatus.Active)
        {
            throw new CommodityNotFoundException(command.CommodityId);
        }

        var ownerExists = await userRepository.ExistsAsync(command.OwnerUserId, ct);
        if (!ownerExists)
        {
            throw new OwnerNotFoundException(command.OwnerUserId);
        }

        logger.LogInformation(
            "AddMaterial commodityId={CommodityId} ownerUserId={OwnerUserId} location={Location} quantity={Quantity} quality={Quality} locationId={LocationId} locationType={LocationType}",
            command.CommodityId, command.OwnerUserId, location, quantity, command.Quality,
            command.LocationId, command.LocationType);

        var (row, isNew) = await repository.AddOrIncrementAsync(
            command.CommodityId, command.OwnerUserId, location, quantity, command.Quality,
            command.LocationId, command.LocationType, ct);

        logger.LogInformation("AddMaterial {Action} rowId={RowId} quantity={Quantity}",
            isNew ? "created" : "incremented", row.Id, row.Quantity);

        return (row, isNew);
    }
}
