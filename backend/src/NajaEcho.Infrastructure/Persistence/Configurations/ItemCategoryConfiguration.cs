using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.ItemCategories;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.ToTable("item_categories", schema: "sc");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UexId).HasColumnName("uex_id").IsRequired();
        builder.Property(c => c.Type).HasColumnName("type").HasMaxLength(128).IsRequired();
        builder.Property(c => c.Section).HasColumnName("section").HasMaxLength(256);
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
        builder.Property(c => c.IsGameRelated).HasColumnName("is_game_related").IsRequired();
        builder.Property(c => c.IsMining).HasColumnName("is_mining").IsRequired();
        builder.Property(c => c.SourceDateAdded).HasColumnName("source_date_added");
        builder.Property(c => c.SourceDateModified).HasColumnName("source_date_modified");

        builder.Property(c => c.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(c => c.ImportedAt).HasColumnName("imported_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(c => c.UexId).IsUnique().HasDatabaseName("ix_item_categories_uex_id");
        builder.HasIndex(c => c.Type).HasDatabaseName("ix_item_categories_type");
        builder.HasIndex(c => c.Section).HasDatabaseName("ix_item_categories_section");
    }
}
