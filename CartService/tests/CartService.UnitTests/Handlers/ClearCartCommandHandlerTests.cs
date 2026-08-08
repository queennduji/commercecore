using CartService.Application.Commands;
using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class ClearCartCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingCart_DeletesIt()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var cart = new Cart { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new ClearCartCommandHandler(cartRepository);
        var result = await handler.Handle(new ClearCartCommand(cart.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        await cartRepository.Received(1).DeleteAsync(cart.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        cartRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new ClearCartCommandHandler(cartRepository);
        var result = await handler.Handle(new ClearCartCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        await cartRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
