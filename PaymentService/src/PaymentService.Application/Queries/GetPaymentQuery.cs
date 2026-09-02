using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using MediatR;

namespace PaymentService.Application.Queries;

/// <summary>Ownership-checked against UserId – same "not found rather than forbidden" pattern
/// used by OrderService's GetOrderQuery.</summary>
public record GetPaymentQuery(Guid PaymentId, Guid UserId) : IRequest<ServiceResult<PaymentDto>>;
