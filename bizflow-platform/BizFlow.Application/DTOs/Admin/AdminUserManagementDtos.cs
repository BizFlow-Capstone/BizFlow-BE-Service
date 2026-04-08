using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Admin;

public class AdminUserQueryParams : PaginationParams
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class AdminManagedUserDto
{
    public Guid AccountId { get; set; }
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

