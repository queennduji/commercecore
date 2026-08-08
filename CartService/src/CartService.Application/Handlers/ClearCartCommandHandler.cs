using CartService.Application.Commands;
using CartService.Application.Common;
using CartService.Application.Interfaces;
using MediatR;

namespace CartService.Application.Handlers;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, ServiceResult<bool>>
{
    private readonly ICartRepository _cartRepository;

    public ClearCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ServiceResult<bool>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
        {
            return ServiceResult<bool>.Failure("Cart not found.");
        }

        await _cartRepository.DeleteAsync(request.CartId, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}
