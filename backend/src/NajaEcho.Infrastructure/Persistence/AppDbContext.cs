using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NajaEcho.Domain.Commodities;
using NajaEcho.Domain.Hangar;
using NajaEcho.Domain.ItemCategories;
using NajaEcho.Domain.Items;
using NajaEcho.Domain.Ships;
using NajaEcho.Domain.Warehouse;
using NajaEcho.Infrastructure.Identity;
using NajaEcho.Infrastructure.Persistence.Configurations;

namespace NajaEcho.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Ship> Ships => Set<Ship>();
    public DbSet<HangarEntry> HangarEntries => Set<HangarEntry>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Commodity> Commodities => Set<Commodity>();
    public DbSet<WarehouseInventoryEntry> WarehouseInventory => Set<WarehouseInventoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
        modelBuilder.ApplyConfiguration(new ShipConfiguration());
        modelBuilder.ApplyConfiguration(new HangarEntryConfiguration());
        modelBuilder.ApplyConfiguration(new ItemCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ItemConfiguration());
        modelBuilder.ApplyConfiguration(new CommodityConfiguration());
        modelBuilder.ApplyConfiguration(new WarehouseInventoryEntryConfiguration());
    }
}
