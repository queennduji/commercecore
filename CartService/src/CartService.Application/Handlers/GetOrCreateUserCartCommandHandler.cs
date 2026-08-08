using CartService.Application.Commands;
using CartService.Application.Common;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Domain.Entities;
using MediatR;

namespace CartService.Application.Handlers;

public class GetOrCreateUserCartCommandHandler : IRequestHandler<GetOrCreateUserCartCommand, ServiceResult<CartDto>>
{
    private readonly ICartRepository _cartRepository;

    public GetOrCreateUserCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ServiceResult<CartDto>> Handle(GetOrCreateUserCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (cart is not null)
        {
            return ServiceResult<CartDto>.Success(cart.ToDto());
        }

        var now = DateTime.UtcNow;
        cart = new Cart
        {
            Id = request.UserId,
            UserId = request.UserId,
            Items = [],
            CreatedAt = now,
            UpdatedAt = now
        };

        await _cartRepository.SaveAsync(cart, cancellationToken);

        return ServiceResult<CartDto>.Success(cart.ToDto());
    }
}
