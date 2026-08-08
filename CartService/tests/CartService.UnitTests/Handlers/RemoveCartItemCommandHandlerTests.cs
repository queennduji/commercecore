using CartService.Application.Commands;
using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class RemoveCartItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingItem_RemovesIt()
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

        var handler = new RemoveCartItemCommandHandler(cartRepository);
        var result = await handler.Handle(new RemoveCartItemCommand(cart.Id, productId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task Handle_UnknownCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        cartRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new RemoveCartItemCommandHandler(cartRepository);
        var result = await handler.Handle(new RemoveCartItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
