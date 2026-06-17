namespace NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;

public sealed record GetMaterialsQuery(
    string? Material,
    Guid? OwnerUserId,
    string? Location,
    int? QualityMin,
    int? QualityMax);
