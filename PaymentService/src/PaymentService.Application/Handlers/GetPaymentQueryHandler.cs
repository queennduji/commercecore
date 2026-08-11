using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Mapping;
using PaymentService.Application.Queries;
using MediatR;

namespace PaymentService.Application.Handlers;

public class GetPaymentQueryHandler : IRequestHandler<GetPaymentQuery, ServiceResult<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<ServiceResult<PaymentDto>> Handle(GetPaymentQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        return payment is null || payment.UserId != request.UserId
            ? ServiceResult<PaymentDto>.Failure("Payment not found.")
            : ServiceResult<PaymentDto>.Success(payment.ToDto());
    }
}
