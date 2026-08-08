using CartService.Application.Commands;
using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class CreateCartCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesEmptyGuestCartAndSavesIt()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var handler = new CreateCartCommandHandler(cartRepository);

        var result = await handler.Handle(new CreateCartCommand(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value!.Id);
        Assert.Null(result.Value.UserId);
        Assert.Empty(result.Value.Items);
        await cartRepository.Received(1).SaveAsync(Arg.Is<Cart>(c => c.Id == result.Value.Id), Arg.Any<CancellationToken>());
    }
}
