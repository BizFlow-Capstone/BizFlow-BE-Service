using System.Linq.Expressions;

namespace BizFlow.Application.Common.Interfaces
{
    public interface ISpecification<T>
    {
        List<Expression<Func<T, bool>>> WhereConditions { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        List<string> IncludeStrings { get; }
        Expression<Func<T, object>>? OrderBy { get; }
        Expression<Func<T, object>>? OrderByDescending { get; }
        int Take { get; }
        int Skip { get; }
        bool IsPagingEnabled { get; }
    }
}
