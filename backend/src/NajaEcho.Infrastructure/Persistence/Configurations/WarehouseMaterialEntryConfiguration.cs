using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class WarehouseMaterialEntryConfiguration : IEntityTypeConfiguration<WarehouseMaterialEntry>
{
    public void Configure(EntityTypeBuilder<WarehouseMaterialEntry> builder)
    {
        builder.ToTable("warehouse_material_inventory", t =>
        {
            t.HasCheckConstraint("ck_warehouse_material_inventory_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_warehouse_material_inventory_quality", "quality >= 1 AND quality <= 1000");
        });
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.CommodityId).HasColumnName("commodity_id").IsRequired();
        builder.Property(w => w.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(w => w.Location).HasColumnName("location").HasMaxLength(200).IsRequired();
        builder.Property(w => w.Quantity).HasColumnName("quantity").HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(w => w.Quality).HasColumnName("quality").HasDefaultValue(500).IsRequired();
        builder.Property(w => w.StationId).HasColumnName("station_id");
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(w => new { w.CommodityId, w.OwnerUserId, w.Location, w.Quality })
            .IsUnique()
            .HasDatabaseName("ux_warehouse_material_inventory_commodity_owner_location_quality");

        builder.HasIndex(w => w.CommodityId).HasDatabaseName("ix_warehouse_material_inventory_commodity_id");
        builder.HasIndex(w => w.OwnerUserId).HasDatabaseName("ix_warehouse_material_inventory_owner_user_id");

        builder.HasOne<NajaEcho.Domain.Commodities.Commodity>()
            .WithMany()
            .HasForeignKey(w => w.CommodityId)
            .HasConstraintName("fk_warehouse_material_inventory_commodity_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Station)
            .WithMany()
            .HasForeignKey(w => w.StationId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
