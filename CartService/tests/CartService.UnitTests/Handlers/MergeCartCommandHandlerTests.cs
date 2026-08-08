using CartService.Application.Commands;
using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class MergeCartCommandHandlerTests
{
    [Fact]
    public async Task Handle_DisjointItems_MergesAllIntoTargetAndDeletesSource()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var userId = Guid.NewGuid();
        var sourceCartId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var sourceCart = new Cart
        {
            Id = sourceCartId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new CartItem { ProductId = productId, Sku = "SKU-1", Name = "Widget", UnitPrice = 9.99m, Quantity = 2, AddedAt = DateTime.UtcNow }]
        };
        cartRepository.GetByIdAsync(sourceCartId, Arg.Any<CancellationToken>()).Returns(sourceCart);
        cartRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new MergeCartCommandHandler(cartRepository);
        var result = await handler.Handle(new MergeCartCommand(userId, sourceCartId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(userId, result.Value!.Id);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(2, item.Quantity);
        await cartRepository.Received(1).SaveAsync(Arg.Is<Cart>(c => c.Id == userId), Arg.Any<CancellationToken>());
        await cartRepository.Received(1).DeleteAsync(sourceCartId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateProductInBothCarts_SumsQuantities()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var userId = Guid.NewGuid();
        var sourceCartId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var sourceCart = new Cart
        {
            Id = sourceCartId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new CartItem { ProductId = productId, Sku = "SKU-1", Name = "Widget", UnitPrice = 9.99m, Quantity = 2, AddedAt = DateTime.UtcNow }]
        };
        var targetCart = new Cart
        {
            Id = userId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new CartItem { ProductId = productId, Sku = "SKU-1", Name = "Widget", UnitPrice = 9.99m, Quantity = 3, AddedAt = DateTime.UtcNow }]
        };
        cartRepository.GetByIdAsync(sourceCartId, Arg.Any<CancellationToken>()).Returns(sourceCart);
        cartRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(targetCart);

        var handler = new MergeCartCommandHandler(cartRepository);
        var result = await handler.Handle(new MergeCartCommand(userId, sourceCartId), CancellationToken.None);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public async Task Handle_UnknownSourceCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        cartRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new MergeCartCommandHandler(cartRepository);
        var result = await handler.Handle(new MergeCartCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
