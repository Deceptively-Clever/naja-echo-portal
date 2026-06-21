using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Locations;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class SpaceStationConfiguration : IEntityTypeConfiguration<SpaceStation>
{
    public void Configure(EntityTypeBuilder<SpaceStation> builder)
    {
        builder.ToTable("space_stations", schema: "sc");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UexId).HasColumnName("uex_id").IsRequired();
        builder.Property(s => s.StarSystemId).HasColumnName("star_system_id").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(s => s.Nickname).HasColumnName("nickname").HasMaxLength(256);
        builder.Property(s => s.IsAvailable).HasColumnName("is_available").IsRequired();
        builder.Property(s => s.IsDecommissioned).HasColumnName("is_decommissioned").IsRequired();
        builder.Property(s => s.IsLandable).HasColumnName("is_landable").IsRequired();
        builder.Property(s => s.HasRefinery).HasColumnName("has_refinery").IsRequired();
        builder.Property(s => s.HasTradeTerminal).HasColumnName("has_trade_terminal").IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.RawData)
            .HasColumnName("raw_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.ImportedAt).HasColumnName("imported_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(s => s.SoftDeletedAt).HasColumnName("soft_deleted_at");

        builder.HasOne(s => s.StarSystem)
            .WithMany()
            .HasForeignKey(s => s.StarSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UexId).IsUnique().HasDatabaseName("ix_space_stations_uex_id");
        builder.HasIndex(s => s.Status).HasDatabaseName("ix_space_stations_status");
        builder.HasIndex(s => new { s.StarSystemId }).HasDatabaseName("ix_space_stations_star_system_id");
        builder.HasIndex(s => new { s.IsAvailable, s.IsDecommissioned, s.Name })
            .HasDatabaseName("ix_space_stations_avail_decomm_name");
    }
}
