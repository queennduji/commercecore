using CartService.Application.Commands;
using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class UpdateCartItemQuantityCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingItem_UpdatesQuantity()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var productId = Guid.NewGuid();
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new CartItem { ProductId = productId, Sku = "SKU-1", Name = "Widget", UnitPrice = 9.99m, Quantity = 1, AddedAt = DateTime.UtcNow }]
        };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new UpdateCartItemQuantityCommandHandler(cartRepository);
        var result = await handler.Handle(new UpdateCartItemQuantityCommand(cart.Id, productId, 5, null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(5, result.Value!.Items.Single().Quantity);
    }

    [Fact]
    public async Task Handle_UnknownCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        cartRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new UpdateCartItemQuantityCommandHandler(cartRepository);
        var result = await handler.Handle(new UpdateCartItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 5, null), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_ProductNotInCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var cart = new Cart { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new UpdateCartItemQuantityCommandHandler(cartRepository);
        var result = await handler.Handle(new UpdateCartItemQuantityCommand(cart.Id, Guid.NewGuid(), 5, null), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
