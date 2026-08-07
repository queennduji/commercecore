using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Queries;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class GetReservationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingReservation_ReturnsDto()
    {
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var reservation = new StockReservation { Id = Guid.NewGuid(), Quantity = 5, Status = ReservationStatus.Active };
        stockReservationRepository.GetByIdAsync(reservation.Id, Arg.Any<CancellationToken>()).Returns(reservation);

        var handler = new GetReservationQueryHandler(stockReservationRepository);
        var result = await handler.Handle(new GetReservationQuery(reservation.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(5, result.Value!.Quantity);
    }

    [Fact]
    public async Task Handle_UnknownReservation_ReturnsFailure()
    {
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        stockReservationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StockReservation?)null);

        var handler = new GetReservationQueryHandler(stockReservationRepository);
        var result = await handler.Handle(new GetReservationQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
