using CartService.Application.Common;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Application.Queries;
using MediatR;

namespace CartService.Application.Handlers;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, ServiceResult<CartDto>>
{
    private readonly ICartRepository _cartRepository;

    public GetCartQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ServiceResult<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
        return cart is null
            ? ServiceResult<CartDto>.Failure("Cart not found.")
            : ServiceResult<CartDto>.Success(cart.ToDto());
    }
}
