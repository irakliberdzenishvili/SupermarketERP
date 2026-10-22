
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

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseManager> WarehouseManagers => Set<WarehouseManager>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable)
                .IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(
                entityType.ClrType, "e");

            var prop = Expression.Property(
                parameter,
                nameof(ISoftDeletable.IsDeleted));

            var compare = Expression.Equal(
                prop,
                Expression.Constant(false));

            var lambda = Expression.Lambda(compare, parameter);

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(lambda);
        }

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Branch>(entity =>
        {
            entity.HasIndex(b => b.BranchManagerId)
                .IsUnique()
                .HasFilter("[BranchManagerId] IS NOT NULL");

            entity.HasOne(b => b.BranchManager)
                .WithMany()
                .HasForeignKey(b => b.BranchManagerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasOne(b => b.Warehouse)
                .WithMany(w => w.Branches)
                .HasForeignKey(b => b.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasIndex(b => b.WarehouseId);
        });

        // Employee configuration
        builder.Entity<Employee>(entity =>
        {
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Employees)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(w => w.Code)
                .IsUnique();

            entity.HasOne(w => w.WarehouseManager)
                .WithOne(wm => wm.Warehouse)
                .HasForeignKey<WarehouseManager>(wm => wm.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WarehouseManager>(entity =>
        {
            entity.HasIndex(wm => wm.EmployeeId)
                .IsUnique();

            entity.HasIndex(wm => wm.WarehouseId)
                .IsUnique();

            entity.HasOne(wm => wm.Employee)
                .WithMany()
                .HasForeignKey(wm => wm.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
