import { ImageOff } from "lucide-react";
import { cn } from "@/lib/utils";

export function ProductImagePlaceholder({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        "flex h-full w-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-muted to-muted/50 text-muted-foreground",
        className,
      )}
    >
      <ImageOff className="size-8 opacity-40" strokeWidth={1.5} />
      <span className="text-xs font-medium opacity-60">No image yet</span>
    </div>
  );
}
