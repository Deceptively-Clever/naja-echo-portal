using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Items;
using NajaEcho.Domain.Warehouse;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class ItemAttributeConfiguration : IEntityTypeConfiguration<ItemAttribute>
{
    public void Configure(EntityTypeBuilder<ItemAttribute> builder)
    {
        builder.ToTable("item_attributes", schema: "sc");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(a => a.UexAttributeId).HasColumnName("uex_attribute_id");
        builder.Property(a => a.UexItemId).HasColumnName("uex_item_id").IsRequired();
        builder.Property(a => a.UexCategoryId).HasColumnName("uex_category_id");
        builder.Property(a => a.UexCategoryAttributeId).HasColumnName("uex_category_attribute_id").IsRequired();
        builder.Property(a => a.AttributeName).HasColumnName("attribute_name").HasMaxLength(256).IsRequired();
        builder.Property(a => a.Value).HasColumnName("value").HasMaxLength(1024);
        builder.Property(a => a.Unit).HasColumnName("unit").HasMaxLength(64);
        builder.Property(a => a.SourceDateAdded).HasColumnName("source_date_added");
        builder.Property(a => a.SourceDateModified).HasColumnName("source_date_modified");
        builder.Property(a => a.FetchedAt).HasColumnName("fetched_at").IsRequired();

        builder.HasIndex(a => new { a.ItemId, a.UexCategoryAttributeId })
            .IsUnique()
            .HasDatabaseName("ux_item_attributes_item_category_attr");

        builder.HasIndex(a => a.ItemId).HasDatabaseName("ix_item_attributes_item_id");

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(a => a.ItemId)
            .HasConstraintName("fk_item_attributes_item_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
