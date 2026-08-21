import { Badge } from "@/components/ui/badge";

export function StockBadge({ available }: { available: number }) {
  if (available > 0) {
    return (
      <Badge
        variant="secondary"
        className="border-green-600/20 bg-green-600/10 text-green-700 dark:text-green-400"
      >
        In stock
      </Badge>
    );
  }

  return (
    <Badge variant="outline" className="text-muted-foreground">
      Out of stock
    </Badge>
  );
}
