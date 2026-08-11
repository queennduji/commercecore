using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Mapping;
using PaymentService.Application.Queries;
using MediatR;

namespace PaymentService.Application.Handlers;

public class ListPaymentsByOrderQueryHandler : IRequestHandler<ListPaymentsByOrderQuery, ServiceResult<IReadOnlyList<PaymentDto>>>
{
    private readonly IPaymentRepository _paymentRepository;

    public ListPaymentsByOrderQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<PaymentDto>>> Handle(ListPaymentsByOrderQuery request, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.ListByOrderIdAsync(request.OrderId, cancellationToken);
        var owned = payments.Where(p => p.UserId == request.UserId).Select(p => p.ToDto()).ToList();
        return ServiceResult<IReadOnlyList<PaymentDto>>.Success(owned);
    }
}
