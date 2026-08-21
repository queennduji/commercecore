"use client";

import { useState } from "react";
import { useAuth } from "@/components/auth/AuthProvider";
import { checkout } from "@/lib/api/orders";
import { ApiError } from "@/lib/api/client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { OrderDto } from "@/types/order";

export function ShippingAddressForm({ onOrderCreated }: { onOrderCreated: (order: OrderDto) => void }) {
  const { accessToken } = useAuth();
  const [recipientName, setRecipientName] = useState("");
  const [street, setStreet] = useState("");
  const [city, setCity] = useState("");
  const [state, setState] = useState("");
  const [zip, setZip] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!accessToken) return;

    setError(null);
    setIsSubmitting(true);
    try {
      // OrderService takes one flat shippingAddress string (max 500 chars), not structured
      // fields — the form is structured for a friendlier UX, then joined here.
      const shippingAddress = `${recipientName}\n${street}\n${city}, ${state} ${zip}`;
      const order = await checkout(accessToken, shippingAddress);
      onOrderCreated(order);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not place your order. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="recipientName">Full name</Label>
        <Input id="recipientName" required value={recipientName} onChange={(e) => setRecipientName(e.target.value)} />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="street">Street address</Label>
        <Input id="street" required value={street} onChange={(e) => setStreet(e.target.value)} />
      </div>
      <div className="grid grid-cols-[1fr_auto_auto] gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="city">City</Label>
          <Input id="city" required value={city} onChange={(e) => setCity(e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="state">State</Label>
          <Input id="state" required className="w-16" value={state} onChange={(e) => setState(e.target.value)} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="zip">ZIP</Label>
          <Input id="zip" required className="w-24" value={zip} onChange={(e) => setZip(e.target.value)} />
        </div>
      </div>
      {error && <p className="text-sm text-destructive">{error}</p>}
      <Button type="submit" disabled={isSubmitting} className="mt-2">
        {isSubmitting ? "Placing order…" : "Place order"}
      </Button>
    </form>
  );
}
