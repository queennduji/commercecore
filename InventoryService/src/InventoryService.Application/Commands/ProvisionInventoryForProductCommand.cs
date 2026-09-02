using InventoryService.Application.Common;
using MediatR;

namespace InventoryService.Application.Commands;

/// <summary>
/// Internal command, not exposed over HTTP: dispatched by the Kafka consumer when CatalogService
/// publishes catalog.product-created.v1, to create a zero-stock InventoryItem for the new product
/// at every currently-active location. Idempotent – safe to run more than once for the same product
/// (e.g. on consumer redelivery after a rebalance).
/// </summary>
public record ProvisionInventoryForProductCommand(Guid ProductId) : IRequest<ServiceResult<bool>>;
