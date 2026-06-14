using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Items;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items", schema: "sc");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.Uuid).HasColumnName("uuid").HasMaxLength(128).IsRequired();
        builder.Property(i => i.UexId).HasColumnName("uex_id").IsRequired();
        builder.Property(i => i.IdParent).HasColumnName("id_parent");
        builder.Property(i => i.IdCategory).HasColumnName("id_category").IsRequired();
        builder.Property(i => i.IdCompany).HasColumnName("id_company");
        builder.Property(i => i.IdVehicle).HasColumnName("id_vehicle");
        builder.Property(i => i.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
        builder.Property(i => i.Section).HasColumnName("section").HasMaxLength(256);
        builder.Property(i => i.Category).HasColumnName("category").HasMaxLength(256);
        builder.Property(i => i.CompanyName).HasColumnName("company_name").HasMaxLength(256);
        builder.Property(i => i.VehicleName).HasColumnName("vehicle_name").HasMaxLength(256);
        builder.Property(i => i.Slug).HasColumnName("slug").HasMaxLength(256);
        builder.Property(i => i.Size).HasColumnName("size").HasMaxLength(64);
        builder.Property(i => i.Color).HasColumnName("color").HasMaxLength(64);
        builder.Property(i => i.Color2).HasColumnName("color2").HasMaxLength(64);
        builder.Property(i => i.UrlStore).HasColumnName("url_store").HasMaxLength(1024);
        builder.Property(i => i.Wiki).HasColumnName("wiki").HasMaxLength(1024);
        builder.Property(i => i.Quality).HasColumnName("quality").HasMaxLength(64);
        builder.Property(i => i.IsExclusivePledge).HasColumnName("is_exclusive_pledge").IsRequired();
        builder.Property(i => i.IsExclusiveSubscriber).HasColumnName("is_exclusive_subscriber").IsRequired();
        builder.Property(i => i.IsExclusiveConcierge).HasColumnName("is_exclusive_concierge").IsRequired();
        builder.Property(i => i.IsCommodity).HasColumnName("is_commodity").IsRequired();
        builder.Property(i => i.IsHarvestable).HasColumnName("is_harvestable").IsRequired();
        builder.Property(i => i.Notification).HasColumnName("notification");
        builder.Property(i => i.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        builder.Property(i => i.SourceDateAdded).HasColumnName("source_date_added");
        builder.Property(i => i.SourceDateModified).HasColumnName("source_date_modified");

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(i => i.ImportedAt).HasColumnName("imported_at").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(i => i.SoftDeletedAt).HasColumnName("soft_deleted_at");

        builder.HasIndex(i => i.Uuid).HasDatabaseName("ix_items_uuid");
        builder.HasIndex(i => i.UexId).HasDatabaseName("ix_items_uex_id");
        builder.HasIndex(i => i.IdCategory).HasDatabaseName("ix_items_id_category");
        builder.HasIndex(i => i.Status).HasDatabaseName("ix_items_status");
        builder.HasIndex(i => new { i.IdCategory, i.UexId }).HasDatabaseName("ix_items_id_category_uex_id");
    }
}
