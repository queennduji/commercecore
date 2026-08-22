"use client";

import { useMemo, useState } from "react";
import { CardElement, Elements, useElements, useStripe } from "@stripe/react-stripe-js";
import { useAuth } from "@/components/auth/AuthProvider";
import { ApiError } from "@/lib/api/client";
import { cancelOrder, payOrder } from "@/lib/api/orders";
import { getStripe } from "@/lib/stripe/client";
import { Button } from "@/components/ui/button";
import { ProductPrice } from "@/components/product/ProductPrice";
import { OrderDto } from "@/types/order";

export function PaymentForm({
  order,
  onPaid,
  onCancelled,
}: {
  order: OrderDto;
  onPaid: (order: OrderDto) => void;
  onCancelled: (order: OrderDto) => void;
}) {
  // Reads NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY (via getStripe()), so this component must never
  // render during SSR/static generation - it's loaded with next/dynamic(..., { ssr: false }) at
  // its only call site (app/checkout/page.tsx) specifically so this render body only ever
  // executes in the browser, where the key is guaranteed to have been checked already.
  const stripePromise = useMemo(() => getStripe(), []);

  return (
    <Elements stripe={stripePromise}>
      <PaymentFormInner order={order} onPaid={onPaid} onCancelled={onCancelled} />
    </Elements>
  );
}

function PaymentFormInner({
  order,
  onPaid,
  onCancelled,
}: {
  order: OrderDto;
  onPaid: (order: OrderDto) => void;
  onCancelled: (order: OrderDto) => void;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const { accessToken } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [isPaying, setIsPaying] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);

  async function handlePay(e: React.FormEvent) {
    e.preventDefault();
    const cardElement = elements?.getElement(CardElement);
    if (!stripe || !cardElement || !accessToken) return;

    setError(null);
    setIsPaying(true);
    try {
      const { paymentMethod, error: stripeError } = await stripe.createPaymentMethod({
        type: "card",
        card: cardElement,
      });

      if (stripeError || !paymentMethod) {
        setError(stripeError?.message ?? "Could not process that card.");
        return;
      }

      // PaymentService's gateway charges with Confirm=true, OffSession=true - fully synchronous,
      // no 3D Secure/requires_action handling needed here. A decline comes back as an ApiError
      // with Stripe's own message; the order stays Pending, so this form can just be retried.
      const paidOrder = await payOrder(accessToken, order.id, paymentMethod.id);
      onPaid(paidOrder);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Payment failed. Please try again.");
    } finally {
      setIsPaying(false);
    }
  }

  async function handleCancel() {
    if (!accessToken) return;
    setError(null);
    setIsCancelling(true);
    try {
      const cancelledOrder = await cancelOrder(accessToken, order.id);
      onCancelled(cancelledOrder);
    } catch {
      setError("Could not cancel this order. Please try again.");
    } finally {
      setIsCancelling(false);
    }
  }

  const isBusy = isPaying || isCancelling;

  return (
    <form onSubmit={handlePay} className="flex flex-col gap-4 rounded-xl border border-border p-6">
      <h2 className="text-sm font-semibold">Payment</h2>
      <div className="rounded-lg border border-input px-3 py-2.5">
        <CardElement options={{ style: { base: { fontSize: "14px" } } }} />
      </div>
      {error && <p className="text-sm text-destructive">{error}</p>}
      <div className="flex items-center gap-3">
        <Button type="submit" disabled={!stripe || isBusy} className="flex-1">
          {isPaying ? "Paying…" : (
            <>
              Pay <ProductPrice price={order.subtotal} className="text-primary-foreground" />
            </>
          )}
        </Button>
        <Button type="button" variant="outline" disabled={isBusy} onClick={handleCancel}>
          {isCancelling ? "Cancelling…" : "Cancel order"}
        </Button>
      </div>
    </form>
  );
}
