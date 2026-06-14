using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Commodities;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class CommodityConfiguration : IEntityTypeConfiguration<Commodity>
{
    public void Configure(EntityTypeBuilder<Commodity> builder)
    {
        builder.ToTable("commodities", schema: "sc");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UexId).HasColumnName("uex_id").IsRequired();
        builder.Property(c => c.Uuid).HasColumnName("uuid").HasMaxLength(128);
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(64);
        builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(256);
        builder.Property(c => c.Kind).HasColumnName("kind").HasMaxLength(128);
        builder.Property(c => c.WeightScu).HasColumnName("weight_scu").HasColumnType("double precision");
        builder.Property(c => c.IdParent).HasColumnName("id_parent");
        builder.Property(c => c.IdItem).HasColumnName("id_item");
        builder.Property(c => c.Wiki).HasColumnName("wiki").HasMaxLength(1024);

        // Location identifiers — raw
        builder.Property(c => c.IdsStarSystemsRaw).HasColumnName("ids_star_systems_raw");
        builder.Property(c => c.IdsPlanetsRaw).HasColumnName("ids_planets_raw");
        builder.Property(c => c.IdsMoonsRaw).HasColumnName("ids_moons_raw");
        builder.Property(c => c.IdsPoiRaw).HasColumnName("ids_poi_raw");
        builder.Property(c => c.IdsOrbitsRaw).HasColumnName("ids_orbits_raw");

        // Location identifiers — parsed integer arrays
        builder.Property(c => c.IdsStarSystems).HasColumnName("ids_star_systems").IsRequired();
        builder.Property(c => c.IdsPlanets).HasColumnName("ids_planets").IsRequired();
        builder.Property(c => c.IdsMoons).HasColumnName("ids_moons").IsRequired();
        builder.Property(c => c.IdsPoi).HasColumnName("ids_poi").IsRequired();
        builder.Property(c => c.IdsOrbits).HasColumnName("ids_orbits").IsRequired();

        // Boolean flags
        builder.Property(c => c.IsAvailable).HasColumnName("is_available").IsRequired();
        builder.Property(c => c.IsAvailableLive).HasColumnName("is_available_live").IsRequired();
        builder.Property(c => c.IsVisible).HasColumnName("is_visible").IsRequired();
        builder.Property(c => c.IsExtractable).HasColumnName("is_extractable").IsRequired();
        builder.Property(c => c.IsMineral).HasColumnName("is_mineral").IsRequired();
        builder.Property(c => c.IsRaw).HasColumnName("is_raw").IsRequired();
        builder.Property(c => c.IsPure).HasColumnName("is_pure").IsRequired();
        builder.Property(c => c.IsRefined).HasColumnName("is_refined").IsRequired();
        builder.Property(c => c.IsRefinable).HasColumnName("is_refinable").IsRequired();
        builder.Property(c => c.IsHarvestable).HasColumnName("is_harvestable").IsRequired();
        builder.Property(c => c.IsBuyable).HasColumnName("is_buyable").IsRequired();
        builder.Property(c => c.IsSellable).HasColumnName("is_sellable").IsRequired();
        builder.Property(c => c.IsTemporary).HasColumnName("is_temporary").IsRequired();
        builder.Property(c => c.IsIllegal).HasColumnName("is_illegal").IsRequired();
        builder.Property(c => c.IsVolatileQt).HasColumnName("is_volatile_qt").IsRequired();
        builder.Property(c => c.IsVolatileTime).HasColumnName("is_volatile_time").IsRequired();
        builder.Property(c => c.IsInert).HasColumnName("is_inert").IsRequired();
        builder.Property(c => c.IsExplosive).HasColumnName("is_explosive").IsRequired();
        builder.Property(c => c.IsBuggy).HasColumnName("is_buggy").IsRequired();
        builder.Property(c => c.IsFuel).HasColumnName("is_fuel").IsRequired();

        // Timestamps
        builder.Property(c => c.SourceDateAdded).HasColumnName("source_date_added");
        builder.Property(c => c.SourceDateModified).HasColumnName("source_date_modified");
        builder.Property(c => c.SourceDateAddedUtc).HasColumnName("source_date_added_utc");
        builder.Property(c => c.SourceDateModifiedUtc).HasColumnName("source_date_modified_utc");

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(c => c.ImportedAt).HasColumnName("imported_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(c => c.SoftDeletedAt).HasColumnName("soft_deleted_at");

        builder.HasIndex(c => c.UexId).IsUnique().HasDatabaseName("ix_commodities_uex_id");
        builder.HasIndex(c => c.Status).HasDatabaseName("ix_commodities_status");
    }
}
