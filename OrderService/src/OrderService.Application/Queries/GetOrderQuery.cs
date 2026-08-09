using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Queries;

/// <summary>Ownership-checked against UserId — returns "not found" rather than "forbidden" for a
/// mismatch, so the endpoint doesn't leak whether an order id exists at all to someone who doesn't
/// own it.</summary>
public record GetOrderQuery(Guid OrderId, Guid UserId) : IRequest<ServiceResult<OrderDto>>;
