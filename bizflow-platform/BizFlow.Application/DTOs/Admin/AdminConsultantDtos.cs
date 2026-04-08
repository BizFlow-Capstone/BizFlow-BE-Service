using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Admin;

public class CreateConsultantRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [MaxLength(200)]
    public string? FullName { get; set; }
}

public class CreateConsultantResponse
{
    public Guid AccountId { get; set; }
    public Guid ProfileId { get; set; }
    public string Email { get; set; } = null!;
}
