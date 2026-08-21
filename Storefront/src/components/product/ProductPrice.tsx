import { formatCurrency } from "@/lib/utils/formatCurrency";
import { cn } from "@/lib/utils";

export function ProductPrice({ price, className }: { price: number; className?: string }) {
  return <span className={cn("font-semibold tabular-nums", className)}>{formatCurrency(price)}</span>;
}
