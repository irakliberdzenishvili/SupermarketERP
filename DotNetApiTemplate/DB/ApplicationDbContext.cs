
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models;
using DotNetApiTemplate.Models.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Models.Data;
using System.Linq.Expressions;

namespace DotNetApiTemplate.DB;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    #region DB SETS

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<BranchWorker> BranchWorkers => Set<BranchWorker>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<WarehouseWorker> WarehouseWorkers => Set<WarehouseWorker>();

    public DbSet<WarehouseManager> WarehouseManagers => Set<WarehouseManager>();

    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region SOFT DELETE QUERY FILTERS

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable)
                .IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(
                entityType.ClrType,
                "e");

            var property = Expression.Property(
                parameter,
                nameof(ISoftDeletable.IsDeleted));

            var comparison = Expression.Equal(
                property,
                Expression.Constant(false));

            var lambda = Expression.Lambda(
                comparison,
                parameter);

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(lambda);
        }

        #endregion

        #region REFRESH TOKENS

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region BRANCHES

        builder.Entity<Branch>(entity =>
        {
            entity.HasIndex(b => b.Code)
                .IsUnique();

            // One branch has one manager.
            // One worker can manage only one branch.

            entity.HasIndex(b => b.BranchManagerId)
                .IsUnique()
                .HasFilter("[BranchManagerId] IS NOT NULL");

            entity.HasOne(b => b.BranchManager)
                .WithMany(w => w.ManagedBranches)
                .HasForeignKey(b => b.BranchManagerId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Many branches can belong to one warehouse.

            entity.HasOne(b => b.Warehouse)
                .WithMany(w => w.Branches)
                .HasForeignKey(b => b.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            entity.HasIndex(b => b.WarehouseId);
        });

        #endregion

        #region BRANCH WORKERS

        builder.Entity<BranchWorker>(entity =>
        {
            // One Identity user has one BranchWorker record.

            entity.HasIndex(w => w.UserId)
                .IsUnique();

            entity.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // One branch has many workers.

            entity.HasOne(w => w.Branch)
                .WithMany(b => b.Workers)
                .HasForeignKey(w => w.BranchId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            entity.HasIndex(w => w.BranchId);
        });

        #endregion

        #region WAREHOUSES

        builder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(w => w.Code)
                .IsUnique();

            // One warehouse has one warehouse manager.

            entity.HasOne(w => w.WarehouseManager)
                .WithOne(wm => wm.Warehouse)
                .HasForeignKey<WarehouseManager>(
                    wm => wm.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        #endregion

        #region WAREHOUSE WORKERS

        builder.Entity<WarehouseWorker>(entity =>
        {
            // One Identity user has one WarehouseWorker record.

            entity.HasIndex(w => w.UserId)
                .IsUnique();

            entity.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // One warehouse has many workers.

            entity.HasOne(w => w.Warehouse)
                .WithMany(wh => wh.Workers)
                .HasForeignKey(w => w.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(w => w.WarehouseId);
        });

        #endregion

        #region WAREHOUSE MANAGERS

        builder.Entity<WarehouseManager>(entity =>
        {
            // One user can manage one warehouse.

            entity.HasIndex(wm => wm.UserId)
                .IsUnique();

            // One warehouse has one manager.

            entity.HasIndex(wm => wm.WarehouseId)
                .IsUnique();

            // WarehouseManager references Identity User,
            // not BranchWorker.

            entity.HasOne(wm => wm.User)
                .WithMany()
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        #endregion
    }
}
