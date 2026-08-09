using OrderService.Application.Interfaces;

namespace OrderService.IntegrationTests.Fixtures;

public class FakeReservation
{
    public required Guid ProductId { get; init; }
    public required Guid LocationId { get; init; }
    public required int Quantity { get; init; }
    public string Status { get; set; } = "Active";
}

/// <summary>Stands in for a real InventoryService — same reasoning as FakeCartServiceClient.
/// Tests seed <see cref="StockByProduct"/> directly and can inspect <see cref="Reservations"/>
/// afterward to assert reserve/release/commit calls actually happened.</summary>
public class FakeInventoryServiceClient : IInventoryServiceClient
{
    public Dictionary<Guid, List<LocationStockSnapshot>> StockByProduct { get; } = [];
    public Dictionary<Guid, FakeReservation> Reservations { get; } = [];

    public Task<IReadOnlyList<LocationStockSnapshot>> GetStockAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        StockByProduct.TryGetValue(productId, out var stock);
        return Task.FromResult<IReadOnlyList<LocationStockSnapshot>>(stock ?? []);
    }

    public Task<Guid?> ReserveAsync(Guid productId, Guid locationId, int quantity, string referenceId, CancellationToken cancellationToken = default)
    {
        var reservationId = Guid.NewGuid();
        Reservations[reservationId] = new FakeReservation { ProductId = productId, LocationId = locationId, Quantity = quantity };
        return Task.FromResult<Guid?>(reservationId);
    }

    public Task<bool> ReleaseAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        if (!Reservations.TryGetValue(reservationId, out var reservation))
        {
            return Task.FromResult(false);
        }

        reservation.Status = "Released";
        return Task.FromResult(true);
    }

    public Task<bool> CommitAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        if (!Reservations.TryGetValue(reservationId, out var reservation))
        {
            return Task.FromResult(false);
        }

        reservation.Status = "Committed";
        return Task.FromResult(true);
    }
}
