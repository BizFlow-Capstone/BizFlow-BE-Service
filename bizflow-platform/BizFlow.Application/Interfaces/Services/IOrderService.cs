using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Order;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderActionResultDto> CreateAsync(Guid userId, CreateOrderRequest request);
        Task<OrderActionResultDto> UpdateAsync(Guid userId, long orderId, UpdateOrderRequest request);
        Task<OrderActionResultDto> CompleteAsync(Guid userId, long orderId, CompleteOrderRequest request);
        Task<OrderDto> CancelAsync(Guid userId, long orderId, CancelOrderRequest request);
        Task<EditCompletedSaveResultDto> EditCompletedSaveAsync(Guid userId, long oldOrderId, UpdateOrderRequest request);
        Task<OrderDto> GetDetailAsync(Guid userId, long orderId);
        Task<PaginatedResponse<OrderDto>> ListAsync(Guid userId, OrderQueryParams query);
    }
}
