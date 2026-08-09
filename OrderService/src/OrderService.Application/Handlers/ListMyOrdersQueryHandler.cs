using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Application.Queries;
using MediatR;

namespace OrderService.Application.Handlers;

public class ListMyOrdersQueryHandler : IRequestHandler<ListMyOrdersQuery, ServiceResult<PagedResult<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;

    public ListMyOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ServiceResult<PagedResult<OrderDto>>> Handle(ListMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _orderRepository.ListByUserIdAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
        var dtos = items.Select(o => o.ToDto()).ToList();
        return ServiceResult<PagedResult<OrderDto>>.Success(new PagedResult<OrderDto>(dtos, request.Page, request.PageSize, totalCount));
    }
}
