using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Accounting
{
    /// <summary>
    /// Result of a replace-when-posted / replace-when-confirmed save (mirrors
    /// <see cref="BizFlow.Application.DTOs.Order.EditCompletedSaveResultDto"/>).
    /// </summary>
    public sealed class PostedRecordReplacementResultDto
    {
        public long OldRecordId { get; set; }
        public ReferenceOptionDto OldRecordStatus { get; set; } = null!;
        public long NewRecordId { get; set; }
        public ReferenceOptionDto NewRecordStatus { get; set; } = null!;
    }
}
