using System.Linq.Expressions;

namespace Keyloop.Application.Specifications;

public abstract class BaseSpecification<TEntity> : ISpecification<TEntity>
    where TEntity : class
{
    private readonly List<Expression<Func<TEntity, object?>>> _includes = [];
    private readonly List<string> _includeStrings = [];

    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public IReadOnlyList<Expression<Func<TEntity, object?>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;
    public Expression<Func<TEntity, object?>>? OrderBy { get; private set; }
    public Expression<Func<TEntity, object?>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool AsNoTracking { get; private set; } = true;

    protected void Where(Expression<Func<TEntity, bool>> criteria) => Criteria = criteria;
    protected void Include(Expression<Func<TEntity, object?>> includeExpression) => _includes.Add(includeExpression);
    protected void Include(string includeString) => _includeStrings.Add(includeString);
    protected void ApplyOrderBy(Expression<Func<TEntity, object?>> orderBy) => OrderBy = orderBy;
    protected void ApplyOrderByDescending(Expression<Func<TEntity, object?>> orderByDescending) => OrderByDescending = orderByDescending;
    protected void ApplyPaging(int skip, int take)
    {
        Skip = Math.Max(skip, 0);
        Take = Math.Max(take, 1);
    }

    protected void TrackChanges() => AsNoTracking = false;
}
