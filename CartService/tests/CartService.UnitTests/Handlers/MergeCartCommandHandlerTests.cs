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

    [Fact]
    public async Task Handle_SourceCartBelongsToAnotherUser_ReturnsFailureAndDoesNotMergeOrDelete()
    {
        // The actual vulnerability this guards: SourceCartId is fully client-supplied, and an
        // authenticated user's cart id equals their own user id - without this check, an attacker
        // could pass a victim's user id as SourceCartId to copy the victim's cart contents into
        // their own cart and have the victim's real cart deleted.
        var cartRepository = Substitute.For<ICartRepository>();
        var callerId = Guid.NewGuid();
        var victimId = Guid.NewGuid();
        var victimCart = new Cart { Id = victimId, UserId = victimId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(victimId, Arg.Any<CancellationToken>()).Returns(victimCart);

        var handler = new MergeCartCommandHandler(cartRepository);
        var result = await handler.Handle(new MergeCartCommand(callerId, victimId), CancellationToken.None);

        Assert.False(result.Succeeded);
        await cartRepository.DidNotReceive().SaveAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>());
        await cartRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
