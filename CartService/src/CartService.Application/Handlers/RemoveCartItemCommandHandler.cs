using CartService.Application.Commands;
using CartService.Application.Common;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using MediatR;

namespace CartService.Application.Handlers;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, ServiceResult<CartDto>>
{
    private readonly ICartRepository _cartRepository;

    public RemoveCartItemCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ServiceResult<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null || (cart.UserId is { } ownerId && ownerId != request.CallerUserId))
        {
            return ServiceResult<CartDto>.Failure("Cart not found.");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item is null)
        {
            return ServiceResult<CartDto>.Failure("Product is not in this cart.");
        }

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;

        await _cartRepository.SaveAsync(cart, cancellationToken);

        return ServiceResult<CartDto>.Success(cart.ToDto());
    }
}
