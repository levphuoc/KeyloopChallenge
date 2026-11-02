using System.Linq.Expressions;

namespace Keyloop.Application.Extensions;

public static class ProjectionExtensions
{
    // Keep DTO projections in SQL; do not materialize entities only to map them afterwards.
    public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TDestination>> projection)
        => query.Select(projection);
}
