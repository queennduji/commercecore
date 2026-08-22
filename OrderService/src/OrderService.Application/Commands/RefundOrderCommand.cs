using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Ops action (see ShipOrderCommand). Valid from Paid/Shipped/Delivered — deliberately
/// does not touch inventory (restocking a post-shipment return is out of scope for now).
///
/// UserId here is an interim tightening, not the real fix for an "ops action" - it stops the
/// concrete vulnerability (any authenticated customer could refund any other customer's order by
/// guessing/learning its id, since this previously had no ownership check at all) using the same
/// caller-must-own-the-order pattern already used by Pay/Cancel. It doesn't fully match the intent
/// in the comment above ("ops action" implies staff acting on someone else's order, not the
/// customer themselves) - that needs real role-based authorization, which this codebase doesn't
/// have yet (Identity's role tables exist but nothing assigns or checks a role anywhere). Revisit
/// once that exists.</summary>
public record RefundOrderCommand(Guid OrderId, Guid UserId) : IRequest<ServiceResult<OrderDto>>;
