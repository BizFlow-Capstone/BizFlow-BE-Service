using BizFlow.Application.DTOs.Accounting;

namespace BizFlow.Application.DTOs.Import
{
    public sealed class ImportUpdateResultDto
    {
        public bool IsReplacement { get; init; }
        public ImportSummaryDto? Import { get; init; }
        public PostedRecordReplacementResultDto? Replacement { get; init; }
    }
}
