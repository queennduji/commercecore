using CartService.Application.Commands;
using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class GetOrCreateUserCartCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingUserCart_ReturnsItWithoutCreating()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var userId = Guid.NewGuid();
        var cart = new Cart { Id = userId, UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new GetOrCreateUserCartCommandHandler(cartRepository);
        var result = await handler.Handle(new GetOrCreateUserCartCommand(userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(userId, result.Value!.Id);
        await cartRepository.DidNotReceive().SaveAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoExistingCart_CreatesOneKeyedByUserId()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var userId = Guid.NewGuid();
        cartRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new GetOrCreateUserCartCommandHandler(cartRepository);
        var result = await handler.Handle(new GetOrCreateUserCartCommand(userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(userId, result.Value!.Id);
        Assert.Equal(userId, result.Value.UserId);
        await cartRepository.Received(1).SaveAsync(Arg.Is<Cart>(c => c.Id == userId), Arg.Any<CancellationToken>());
    }
}
