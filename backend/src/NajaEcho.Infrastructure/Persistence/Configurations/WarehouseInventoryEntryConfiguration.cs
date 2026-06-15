using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class WarehouseInventoryEntryConfiguration : IEntityTypeConfiguration<WarehouseInventoryEntry>
{
    public void Configure(EntityTypeBuilder<WarehouseInventoryEntry> builder)
    {
        builder.ToTable("warehouse_inventory", t =>
        {
            t.HasCheckConstraint("ck_warehouse_inventory_quantity", "quantity >= 1");
            t.HasCheckConstraint("ck_warehouse_inventory_quality", "quality >= 1 AND quality <= 1000");
        });
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(w => w.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(w => w.Location).HasColumnName("location").HasMaxLength(200).IsRequired();
        builder.Property(w => w.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(w => w.Quality).HasColumnName("quality").HasDefaultValue(500).IsRequired();
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(w => new { w.ItemId, w.OwnerUserId, w.Location })
            .IsUnique()
            .HasDatabaseName("ux_warehouse_inventory_item_owner_location");

        builder.HasIndex(w => w.ItemId).HasDatabaseName("ix_warehouse_inventory_item_id");
        builder.HasIndex(w => w.OwnerUserId).HasDatabaseName("ix_warehouse_inventory_owner_user_id");

        builder.HasOne<NajaEcho.Domain.Items.Item>()
            .WithMany()
            .HasForeignKey(w => w.ItemId)
            .HasConstraintName("fk_warehouse_inventory_item_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
