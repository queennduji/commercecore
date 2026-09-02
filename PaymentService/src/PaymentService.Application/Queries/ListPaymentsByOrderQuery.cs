using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using MediatR;

namespace PaymentService.Application.Queries;

/// <summary>Results are filtered to payments owned by UserId – an order not belonging to the
/// caller simply yields an empty list rather than an error, so this endpoint doesn't need a
/// separate ownership-failure branch.</summary>
public record ListPaymentsByOrderQuery(Guid OrderId, Guid UserId) : IRequest<ServiceResult<IReadOnlyList<PaymentDto>>>;
