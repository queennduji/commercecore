using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using MediatR;

namespace OrderService.Application.Handlers;

public class MarkOrderPaidCommandHandler : IRequestHandler<MarkOrderPaidCommand, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public MarkOrderPaidCommandHandler(IOrderRepository orderRepository, IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<OrderDto>> Handle(MarkOrderPaidCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.UserId != request.UserId)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            return ServiceResult<OrderDto>.Failure($"Cannot mark an order as paid from status {order.Status}.");
        }

        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Paid;
        order.UpdatedAt = now;
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishOrderPaidAsync(new OrderPaidEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            PaidAt = now
        }, cancellationToken);

        return ServiceResult<OrderDto>.Success(order.ToDto());
    }
}
