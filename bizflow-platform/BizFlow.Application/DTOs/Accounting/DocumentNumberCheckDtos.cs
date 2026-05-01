namespace BizFlow.Application.DTOs.Accounting;

public class CheckDocumentNumberExistsQuery
{
    public string? DocumentNumber { get; set; }
    public long? ExcludeCostId { get; set; }
    public long? ExcludeRevenueId { get; set; }
}

public class DocumentNumberExistsResponseDto
{
    public bool Exists { get; set; }
}
