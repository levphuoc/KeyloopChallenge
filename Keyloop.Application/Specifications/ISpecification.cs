using System.Linq.Expressions;

namespace Keyloop.Application.Specifications;

public interface ISpecification<TEntity>
    where TEntity : class
{
    Expression<Func<TEntity, bool>>? Criteria { get; }
    IReadOnlyList<Expression<Func<TEntity, object?>>> Includes { get; }
    IReadOnlyList<string> IncludeStrings { get; }
    Expression<Func<TEntity, object?>>? OrderBy { get; }
    Expression<Func<TEntity, object?>>? OrderByDescending { get; }
    int? Skip { get; }
    int? Take { get; }
    bool AsNoTracking { get; }
}
