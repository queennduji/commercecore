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
        if (cart is null)
        {
            return ServiceResult<CartDto>.Failure("Cart not found.");
        }

        // A guest cart (UserId null) is fine to serve anonymously - its unguessable random id is
        // the authorization. A persistent user cart's id IS the owning user's id (see Cart.cs),
        // not a secret, so it needs an actual ownership check: same "not found" response for
        // "doesn't exist" and "exists but isn't yours" so this doesn't become an oracle for
        // guessing which user ids have carts.
        if (cart.UserId is { } ownerId && ownerId != request.CallerUserId)
        {
            return ServiceResult<CartDto>.Failure("Cart not found.");
        }

        return ServiceResult<CartDto>.Success(cart.ToDto());
    }
}
