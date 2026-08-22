using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Ops action (see ShipOrderCommand). Valid from Paid/Shipped/Delivered — deliberately
/// does not touch inventory (restocking a post-shipment return is out of scope for now).
///
/// No UserId/ownership check here on purpose: authorization for this one is enforced at the
/// controller via [Authorize(Roles = "Admin")], not by comparing against the order's owner - an
/// admin refunding a customer's order was never going to *be* that order's owner. (An interim
/// version of this command briefly required UserId == order.UserId, before Admin-role
/// infrastructure existed; removed now that it does.)</summary>
public record RefundOrderCommand(Guid OrderId) : IRequest<ServiceResult<OrderDto>>;
