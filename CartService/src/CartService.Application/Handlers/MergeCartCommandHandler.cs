using CartService.Application.Commands;
using CartService.Application.Common;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Domain.Entities;
using MediatR;

namespace CartService.Application.Handlers;

public class MergeCartCommandHandler : IRequestHandler<MergeCartCommand, ServiceResult<CartDto>>
{
    private readonly ICartRepository _cartRepository;

    public MergeCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ServiceResult<CartDto>> Handle(MergeCartCommand request, CancellationToken cancellationToken)
    {
        var sourceCart = await _cartRepository.GetByIdAsync(request.SourceCartId, cancellationToken);
        if (sourceCart is null)
        {
            return ServiceResult<CartDto>.Failure("Source cart not found.");
        }

        // The validator only checks SourceCartId != the caller's own id - it can't see the fetched
        // cart's UserId, so this is where the real guard against merging (and then deleting)
        // someone else's persistent cart has to live. Only a genuine guest cart (UserId null) is a
        // legitimate merge source: this endpoint exists specifically for "fold the cart I was
        // using before I logged in into my account," not for pulling in an arbitrary other user's
        // cart by guessing their user id.
        if (sourceCart.UserId is not null)
        {
            return ServiceResult<CartDto>.Failure("Source cart is not a guest cart.");
        }

        var targetCart = await _cartRepository.GetByIdAsync(request.UserId, cancellationToken);
        var now = DateTime.UtcNow;

        if (targetCart is null)
        {
            targetCart = new Cart
            {
                Id = request.UserId,
                UserId = request.UserId,
                Items = [],
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        foreach (var sourceItem in sourceCart.Items)
        {
            var existing = targetCart.Items.FirstOrDefault(i => i.ProductId == sourceItem.ProductId);
            if (existing is not null)
            {
                existing.Quantity += sourceItem.Quantity;
            }
            else
            {
                targetCart.Items.Add(new CartItem
                {
                    ProductId = sourceItem.ProductId,
                    Sku = sourceItem.Sku,
                    Name = sourceItem.Name,
                    UnitPrice = sourceItem.UnitPrice,
                    Quantity = sourceItem.Quantity,
                    AddedAt = sourceItem.AddedAt
                });
            }
        }

        targetCart.UpdatedAt = now;

        await _cartRepository.SaveAsync(targetCart, cancellationToken);
        await _cartRepository.DeleteAsync(request.SourceCartId, cancellationToken);

        return ServiceResult<CartDto>.Success(targetCart.ToDto());
    }
}
