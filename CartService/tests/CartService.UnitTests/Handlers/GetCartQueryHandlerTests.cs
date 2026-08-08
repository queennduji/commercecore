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
        var result = await handler.Handle(new GetCartQuery(cart.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(cart.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_UnknownCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        cartRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new GetCartQueryHandler(cartRepository);
        var result = await handler.Handle(new GetCartQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
