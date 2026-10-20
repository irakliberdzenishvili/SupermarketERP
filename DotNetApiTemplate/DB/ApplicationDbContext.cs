
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Models.Data.Branch;
using System.Linq.Expressions;

protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // Global soft-delete filter
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

    // Refresh tokens
    builder.Entity<RefreshToken>()
        .HasIndex(x => x.TokenHash)
        .IsUnique();

    builder.Entity<RefreshToken>()
        .HasOne(x => x.User)
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Branch configuration
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
    });

    // Employee -> Branch relationship
    builder.Entity<Employee>()
        .HasOne(e => e.Branch)
        .WithMany(b => b.Employees)
        .HasForeignKey(e => e.BranchId)
        .OnDelete(DeleteBehavior.Restrict);
}
