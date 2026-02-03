using BizFlow.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Specifications
{
    public class SpecificationEvaluator<TEntity> where TEntity : class
    {
        public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> inputQuery, ISpecification<TEntity> spec)
        {
            var query = inputQuery;

            // modify the IQueryable using the specification's criteria expressions
            query = spec.WhereConditions.Aggregate(query,
                                    (current, criteria) => current.Where(criteria));

            // aggregates includes
            query = spec.Includes.Aggregate(query,
                                    (current, include) => current.Include(include));

            // aggregates string-based includes
            query = spec.IncludeStrings.Aggregate(query,
                                    (current, include) => current.Include(include));

            // Apply ordering
            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
            }

            // Apply paging
            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }

            return query;
        }
    }
}
