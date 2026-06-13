using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NajaEcho.Domain.Hangar;

namespace NajaEcho.Infrastructure.Persistence.Configurations;

public sealed class HangarEntryConfiguration : IEntityTypeConfiguration<HangarEntry>
{
    public void Configure(EntityTypeBuilder<HangarEntry> builder)
    {
        builder.ToTable("hangar_entries");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(h => h.ShipId).HasColumnName("ship_id").IsRequired();
        builder.Property(h => h.AddedAt).HasColumnName("added_at").IsRequired();

        builder.HasIndex(h => new { h.UserId, h.ShipId })
            .IsUnique()
            .HasDatabaseName("ux_hangar_entries_user_ship");

        builder.HasIndex(h => h.ShipId).HasDatabaseName("ix_hangar_entries_ship_id");
        builder.HasIndex(h => h.UserId).HasDatabaseName("ix_hangar_entries_user_id");

        builder.HasOne<NajaEcho.Domain.Ships.Ship>()
            .WithMany()
            .HasForeignKey(h => h.ShipId)
            .HasConstraintName("fk_hangar_entries_ship_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
