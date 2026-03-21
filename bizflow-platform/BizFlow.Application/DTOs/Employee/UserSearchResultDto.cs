namespace BizFlow.Application.DTOs.Employee
{
    public class UserSearchResultDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsAlreadyHired { get; set; }
    }
}
