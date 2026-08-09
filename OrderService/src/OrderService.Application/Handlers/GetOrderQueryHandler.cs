using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Application.Queries;
using MediatR;

namespace OrderService.Application.Handlers;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ServiceResult<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        return order is null || order.UserId != request.UserId
            ? ServiceResult<OrderDto>.Failure("Order not found.")
            : ServiceResult<OrderDto>.Success(order.ToDto());
    }
}
