import { loadStripe, Stripe } from "@stripe/stripe-js";
import { requiredStripePublishableKey } from "@/lib/env";

// loadStripe() should only ever be called once per page load (it injects Stripe.js's own
// <script> tag) — the standard fix is memoizing the promise at module scope and returning the
// same one on every call.
let stripePromise: Promise<Stripe | null> | undefined;

export function getStripe(): Promise<Stripe | null> {
  if (!stripePromise) {
    stripePromise = loadStripe(requiredStripePublishableKey());
  }
  return stripePromise;
}
