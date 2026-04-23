using BizFlow.Application.DTOs.Accounting;

namespace BizFlow.Application.DTOs.Cost
{
    public sealed class ManualCostUpdateResponseDto
    {
        public bool IsReplacement { get; init; }
        public CostDto? Cost { get; init; }
        public PostedRecordReplacementResultDto? Replacement { get; init; }
    }
}
