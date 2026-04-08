using BizFlow.Application.Common.Specifications;
using BizFlow.Application.Common.Utilities;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Specifications.AdminUsers;

public class AdminUserSearchSpec : BaseSpecification<Account>
{
    public AdminUserSearchSpec(AdminUserQueryParams query, bool isCount = false)
        : base(a => a.DeletedAt == null && a.Role.Name.ToLower() != "admin")
    {
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim().ToLower();
            AddCriteria(a => a.Role.Name.ToLower() == role);
        }

        if (query.IsActive.HasValue)
        {
            var isActive = query.IsActive.Value;
            AddCriteria(a => (a.IsActive ?? false) == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchRaw = query.Search.Trim();
            var searchLower = searchRaw.ToLower();
            var phoneVariants = PhoneSearchNormalizer.GetSearchVariants(searchRaw).ToArray();

            AddCriteria(a =>
                (a.Profile != null && a.Profile.FullName.ToLower().Contains(searchLower)) ||
                a.Credentials.Any(c => c.Type == "email" && c.Identifier.ToLower().Contains(searchLower)) ||
                (phoneVariants.Length > 0 &&
                 a.Credentials.Any(c =>
                     c.Type == "phone" &&
                     phoneVariants.Any(v => c.Identifier.Contains(v)))));
        }

        if (!isCount)
        {
            AddOrderByDescending(a => a.CreatedAt);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;
            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }
    }
}
