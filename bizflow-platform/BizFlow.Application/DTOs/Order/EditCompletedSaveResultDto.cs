using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Order
{
    public class EditCompletedSaveResultDto
    {
        public bool RequiresConfirmation { get; set; }
        public List<string>? Warnings { get; set; }
        public long OldOrderId { get; set; }
        /// <summary>
        /// Old order status. <c>Code</c> ∈ {<c>pending</c>, <c>completed</c>, <c>cancelled</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto OldOrderStatus { get; set; } = null!;
        public long NewOrderId { get; set; }
        /// <summary>
        /// New order status. <c>Code</c> ∈ {<c>pending</c>, <c>completed</c>, <c>cancelled</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto NewOrderStatus { get; set; } = null!;
    }
}
