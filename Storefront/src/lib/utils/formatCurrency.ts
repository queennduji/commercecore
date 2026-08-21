// PaymentService hardcodes "usd" throughout (no multi-currency support in Catalog either),
// so a fixed USD formatter is fine for now — revisit if that ever changes.
const formatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
});

export function formatCurrency(amount: number): string {
  return formatter.format(amount);
}
