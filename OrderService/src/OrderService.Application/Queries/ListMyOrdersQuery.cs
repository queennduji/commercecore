using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Queries;

public record ListMyOrdersQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<ServiceResult<PagedResult<OrderDto>>>;
