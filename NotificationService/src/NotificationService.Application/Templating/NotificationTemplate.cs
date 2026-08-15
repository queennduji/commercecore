using NotificationService.Domain.Entities;

namespace NotificationService.Application.Templating;

/// <summary>Deliberately plain interpolated HTML, no external templating engine — matches this
/// platform's general preference for the simplest thing that works (e.g. PaymentService's
/// description strings) over pulling in a templating library for seven fixed messages.</summary>
public static class NotificationTemplate
{
    public static (string Subject, string Body) Render(NotificationType type, Guid orderId, string? detail) => type switch
    {
        NotificationType.OrderCreated => (
            $"Order {orderId} received",
            $"<p>Thanks for your order! We've received order <strong>{orderId}</strong> and it's being processed.</p>"),
        NotificationType.OrderPaid => (
            $"Payment received for order {orderId}",
            $"<p>We've received your payment for order <strong>{orderId}</strong>. It's being prepared for shipment.</p>"),
        NotificationType.OrderShipped => (
            $"Order {orderId} has shipped",
            $"<p>Your order <strong>{orderId}</strong> is on its way!</p>"),
        NotificationType.OrderDelivered => (
            $"Order {orderId} delivered",
            $"<p>Your order <strong>{orderId}</strong> has been delivered. Enjoy!</p>"),
        NotificationType.OrderCancelled => (
            $"Order {orderId} cancelled",
            $"<p>Your order <strong>{orderId}</strong> has been cancelled.</p>"),
        NotificationType.OrderRefunded => (
            $"Order {orderId} refunded",
            $"<p>Your order <strong>{orderId}</strong> has been refunded.</p>"),
        NotificationType.PaymentFailed => (
            $"Payment failed for order {orderId}",
            $"<p>We couldn't process your payment for order <strong>{orderId}</strong>{(string.IsNullOrEmpty(detail) ? "." : $": {detail}.")} Please try again.</p>"),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>Short plain-text form for SMS — no subject line, no HTML, terse enough to fit a
    /// single text message.</summary>
    public static string RenderSms(NotificationType type, Guid orderId, string? detail) => type switch
    {
        NotificationType.OrderCreated => $"CommerceCore: order {orderId} received and being processed.",
        NotificationType.OrderPaid => $"CommerceCore: payment received for order {orderId}.",
        NotificationType.OrderShipped => $"CommerceCore: order {orderId} has shipped!",
        NotificationType.OrderDelivered => $"CommerceCore: order {orderId} has been delivered. Enjoy!",
        NotificationType.OrderCancelled => $"CommerceCore: order {orderId} has been cancelled.",
        NotificationType.OrderRefunded => $"CommerceCore: order {orderId} has been refunded.",
        NotificationType.PaymentFailed => $"CommerceCore: payment failed for order {orderId}{(string.IsNullOrEmpty(detail) ? "" : $" ({detail})")}. Please try again.",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
