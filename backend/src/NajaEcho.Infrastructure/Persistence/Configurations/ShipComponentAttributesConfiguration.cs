using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Items;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class ShipComponentAttributesConfiguration : IEntityTypeConfiguration<ShipComponentAttributes>
{
    public void Configure(EntityTypeBuilder<ShipComponentAttributes> builder)
    {
        builder.ToTable("ship_component_attributes", schema: "sc");
        builder.HasKey(s => s.ItemId);

        builder.Property(s => s.ItemId).HasColumnName("item_id");
        builder.Property(s => s.Class).HasColumnName("class").HasMaxLength(128);
        builder.Property(s => s.Size).HasColumnName("size");
        builder.Property(s => s.Grade).HasColumnName("grade").HasMaxLength(128);
        builder.Property(s => s.AttributesFetchedAt).HasColumnName("attributes_fetched_at").IsRequired();

        builder.HasIndex(s => s.Class).HasDatabaseName("ix_ship_component_attributes_class");
        builder.HasIndex(s => s.Size).HasDatabaseName("ix_ship_component_attributes_size");
        builder.HasIndex(s => s.Grade).HasDatabaseName("ix_ship_component_attributes_grade");

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .HasConstraintName("fk_ship_component_attributes_item_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
