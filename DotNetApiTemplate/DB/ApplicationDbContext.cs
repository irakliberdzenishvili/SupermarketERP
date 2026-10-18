using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models;
using DotNetApiTemplate.Models.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Models.Data;
using System.Linq.Expressions;
using System.Reflection.Emit;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var prop = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var compare = Expression.Equal(prop, Expression.Constant(false));
            var lambda = Expression.Lambda(compare, parameter);
            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }

        builder.Entity<RefreshToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}