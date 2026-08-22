using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Application.Queries;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class GetCartQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingCart_ReturnsDto()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var cart = new Cart { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new GetCartQueryHandler(cartRepository);
        var result = await handler.Handle(new GetCartQuery(cart.Id, null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(cart.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_UnknownCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        cartRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new GetCartQueryHandler(cartRepository);
        var result = await handler.Handle(new GetCartQuery(Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_PersistentCartWrongCaller_ReturnsFailure()
    {
        // The actual IDOR this fixes: an authenticated user's cart id IS their own user id (not a
        // secret), so without this check anyone who knows that id could read their cart anonymously.
        var cartRepository = Substitute.For<ICartRepository>();
        var ownerId = Guid.NewGuid();
        var cart = new Cart { Id = ownerId, UserId = ownerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new GetCartQueryHandler(cartRepository);
        var result = await handler.Handle(new GetCartQuery(cart.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_PersistentCartCorrectCaller_ReturnsDto()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var ownerId = Guid.NewGuid();
        var cart = new Cart { Id = ownerId, UserId = ownerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new GetCartQueryHandler(cartRepository);
        var result = await handler.Handle(new GetCartQuery(cart.Id, ownerId), CancellationToken.None);

        Assert.True(result.Succeeded);
    }
}
