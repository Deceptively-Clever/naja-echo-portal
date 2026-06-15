using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class ShipComponentAttributesConfiguration : IEntityTypeConfiguration<ShipComponentAttributes>
{
    public void Configure(EntityTypeBuilder<ShipComponentAttributes> builder)
    {
        // Read-only view derived from sc.item_attributes (DDL lives in the migration).
        builder.ToView("ship_component_attributes", schema: "sc");
        builder.HasKey(s => s.ItemId);

        builder.Property(s => s.ItemId).HasColumnName("item_id");
        builder.Property(s => s.Class).HasColumnName("class").HasMaxLength(128);
        builder.Property(s => s.Size).HasColumnName("size");
        builder.Property(s => s.Grade).HasColumnName("grade").HasMaxLength(128);
        builder.Property(s => s.AttributesFetchedAt).HasColumnName("attributes_fetched_at").IsRequired();
    }
}
