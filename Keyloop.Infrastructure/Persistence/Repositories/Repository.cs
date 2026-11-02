using System.Linq.Expressions;
using Keyloop.Application.Abstractions.Persistence;
using Keyloop.Application.Commons;
using Keyloop.Application.Specifications;
using Keyloop.Domain.Entities;
using Keyloop.Infrastructure.Persistence.Context;
using Keyloop.Infrastructure.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Infrastructure.Persistence.Repositories;

public class Repository<TEntity, TKey>(AppDbContext context) : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected DbSet<TEntity> Set { get; } = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await Set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => await SpecificationEvaluator.GetQuery(Set.AsQueryable(), specification)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
        => await SpecificationEvaluator.GetQuery(Set.AsQueryable(), specification)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<TProjection>> ProjectPagedAsync<TProjection>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TProjection>> projection,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = Set.AsQueryable();
        if (specification.Criteria is not null)
            filteredQuery = filteredQuery.Where(specification.Criteria);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var query = SpecificationEvaluator.GetQuery(filteredQuery, specification);
        var items = await query.Select(projection).ToListAsync(cancellationToken);

        var pageSize = specification.Take ?? items.Count;
        var page = specification.Skip.HasValue && pageSize > 0
            ? specification.Skip.Value / pageSize + 1
            : 1;

        return new PagedResult<TProjection>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = Set.AsNoTracking().AsQueryable();
        if (predicate is not null)
            query = query.Where(predicate);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public IQueryable<TEntity> Query() => Set.AsQueryable();

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => await Set.AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity) => Set.Update(entity);

    public void Remove(TEntity entity) => Set.Remove(entity);

    public async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
        => await Set.FindAsync([id], cancellationToken) is not null;
}
