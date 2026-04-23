using BizFlow.Application.DTOs.Accounting;

namespace BizFlow.Application.DTOs.Revenue
{
    public sealed class ManualRevenueUpdateResponseDto
    {
        public bool IsReplacement { get; init; }
        public RevenueDto? Revenue { get; init; }
        public PostedRecordReplacementResultDto? Replacement { get; init; }
    }
}
