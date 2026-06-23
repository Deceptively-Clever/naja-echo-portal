namespace NajaEcho.Application.Features.Warehouse.TransferInventoryItem;

public sealed record TransferInventoryItemCommand(Guid RowId, Guid LocationId, string LocationType);
