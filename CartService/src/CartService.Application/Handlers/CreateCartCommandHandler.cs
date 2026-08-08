using CartService.Application.Commands;
using CartService.Application.Common;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Domain.Entities;
using MediatR;

namespace CartService.Application.Handlers;

public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, ServiceResult<CartDto>>
{
    private readonly ICartRepository _cartRepository;

    public CreateCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ServiceResult<CartDto>> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = null,
            Items = [],
            CreatedAt = now,
            UpdatedAt = now
        };

        await _cartRepository.SaveAsync(cart, cancellationToken);

        return ServiceResult<CartDto>.Success(cart.ToDto());
    }
}
