using System.Reflection;
using Keyloop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Infrastructure.Persistence.Context;

internal static class ModelBuilderExtensions
{
    public static void ApplyAuditableFieldLengths(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.Property(nameof(IAuditableEntity.CreatedBy)).HasMaxLength(256);
            builder.Property(nameof(IAuditableEntity.UpdatedBy)).HasMaxLength(256);

            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                builder.Property(nameof(ISoftDeletable.DeletedBy)).HasMaxLength(256);
        }
    }

    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            typeof(ModelBuilderExtensions)
                .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(null, [modelBuilder]);
        }
    }

    public static void ApplyConcurrencyTokens(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IHasConcurrencyToken).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IHasConcurrencyToken.RowVersion))
                .IsRowVersion();
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
        => modelBuilder.Entity<TEntity>().HasQueryFilter(entity => !entity.IsDeleted);
}
