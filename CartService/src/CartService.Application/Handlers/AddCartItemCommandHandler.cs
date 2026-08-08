using CartService.Application.Commands;
using CartService.Application.Common;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Domain.Entities;
using MediatR;

namespace CartService.Application.Handlers;

public class AddCartItemCommandHandler : IRequestHandler<AddCartItemCommand, ServiceResult<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICatalogServiceClient _catalogServiceClient;

    public AddCartItemCommandHandler(ICartRepository cartRepository, ICatalogServiceClient catalogServiceClient)
    {
        _cartRepository = cartRepository;
        _catalogServiceClient = catalogServiceClient;
    }

    public async Task<ServiceResult<CartDto>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
        {
            return ServiceResult<CartDto>.Failure("Cart not found.");
        }

        var product = await _catalogServiceClient.GetProductAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<CartDto>.Failure("Product not found.");
        }

        if (!string.Equals(product.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<CartDto>.Failure("Product is not currently available for purchase.");
        }

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        var now = DateTime.UtcNow;

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.ProductId,
                Sku = product.Sku,
                Name = product.Name,
                UnitPrice = product.Price,
                Quantity = request.Quantity,
                AddedAt = now
            });
        }

        cart.UpdatedAt = now;
        await _cartRepository.SaveAsync(cart, cancellationToken);

        return ServiceResult<CartDto>.Success(cart.ToDto());
    }
}
