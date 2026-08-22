using CartService.Application.Commands;
using CartService.Application.Handlers;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using NSubstitute;

namespace CartService.UnitTests.Handlers;

public class AddCartItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewProduct_AddsLineItemWithSnapshottedPrice()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var catalogServiceClient = Substitute.For<ICatalogServiceClient>();

        var cart = new Cart { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var productId = Guid.NewGuid();
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);
        catalogServiceClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new CatalogProductSnapshot(productId, "SKU-1", "Widget", 9.99m, "Active"));

        var handler = new AddCartItemCommandHandler(cartRepository, catalogServiceClient);
        var result = await handler.Handle(new AddCartItemCommand(cart.Id, productId, 2, null), CancellationToken.None);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(9.99m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(19.98m, item.LineTotal);
        await cartRepository.Received(1).SaveAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductAlreadyInCart_IncrementsQuantityInstead()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var catalogServiceClient = Substitute.For<ICatalogServiceClient>();

        var productId = Guid.NewGuid();
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new CartItem { ProductId = productId, Sku = "SKU-1", Name = "Widget", UnitPrice = 9.99m, Quantity = 1, AddedAt = DateTime.UtcNow }]
        };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);
        catalogServiceClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new CatalogProductSnapshot(productId, "SKU-1", "Widget", 9.99m, "Active"));

        var handler = new AddCartItemCommandHandler(cartRepository, catalogServiceClient);
        var result = await handler.Handle(new AddCartItemCommand(cart.Id, productId, 3, null), CancellationToken.None);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(4, item.Quantity);
    }

    [Fact]
    public async Task Handle_UnknownCart_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var catalogServiceClient = Substitute.For<ICatalogServiceClient>();
        cartRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new AddCartItemCommandHandler(cartRepository, catalogServiceClient);
        var result = await handler.Handle(new AddCartItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1, null), CancellationToken.None);

        Assert.False(result.Succeeded);
        await catalogServiceClient.DidNotReceive().GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var catalogServiceClient = Substitute.For<ICatalogServiceClient>();

        var cart = new Cart { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);
        catalogServiceClient.GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CatalogProductSnapshot?)null);

        var handler = new AddCartItemCommandHandler(cartRepository, catalogServiceClient);
        var result = await handler.Handle(new AddCartItemCommand(cart.Id, Guid.NewGuid(), 1, null), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }

    [Fact]
    public async Task Handle_InactiveProduct_ReturnsFailure()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var catalogServiceClient = Substitute.For<ICatalogServiceClient>();

        var cart = new Cart { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var productId = Guid.NewGuid();
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);
        catalogServiceClient.GetProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new CatalogProductSnapshot(productId, "SKU-1", "Widget", 9.99m, "Draft"));

        var handler = new AddCartItemCommandHandler(cartRepository, catalogServiceClient);
        var result = await handler.Handle(new AddCartItemCommand(cart.Id, productId, 1, null), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_PersistentCartWrongCaller_ReturnsFailureAndDoesNotCallCatalog()
    {
        var cartRepository = Substitute.For<ICartRepository>();
        var catalogServiceClient = Substitute.For<ICatalogServiceClient>();
        var ownerId = Guid.NewGuid();
        var cart = new Cart { Id = ownerId, UserId = ownerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        cartRepository.GetByIdAsync(cart.Id, Arg.Any<CancellationToken>()).Returns(cart);

        var handler = new AddCartItemCommandHandler(cartRepository, catalogServiceClient);
        var result = await handler.Handle(new AddCartItemCommand(cart.Id, Guid.NewGuid(), 1, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        await catalogServiceClient.DidNotReceive().GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
