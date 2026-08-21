import Link from "next/link";
import { cn } from "@/lib/utils";

const pagerButtonClass =
  "inline-flex h-8 items-center justify-center rounded-lg border border-border px-3 text-sm font-medium transition-colors";

export function Pagination({
  page,
  pageSize,
  totalCount,
  categoryId,
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  categoryId?: string;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalPages <= 1) {
    return null;
  }

  const hrefFor = (targetPage: number) => {
    const params = new URLSearchParams();
    if (categoryId) params.set("categoryId", categoryId);
    if (targetPage > 1) params.set("page", String(targetPage));
    const qs = params.toString();
    return `/products${qs ? `?${qs}` : ""}`;
  };

  const hasPrevious = page > 1;
  const hasNext = page < totalPages;

  return (
    <nav aria-label="Pagination" className="flex items-center justify-center gap-3">
      {hasPrevious ? (
        <Link href={hrefFor(page - 1)} className={cn(pagerButtonClass, "hover:bg-muted")}>
          Previous
        </Link>
      ) : (
        <span aria-disabled className={cn(pagerButtonClass, "cursor-not-allowed text-muted-foreground opacity-50")}>
          Previous
        </span>
      )}
      <span className="text-sm text-muted-foreground">
        Page {page} of {totalPages}
      </span>
      {hasNext ? (
        <Link href={hrefFor(page + 1)} className={cn(pagerButtonClass, "hover:bg-muted")}>
          Next
        </Link>
      ) : (
        <span aria-disabled className={cn(pagerButtonClass, "cursor-not-allowed text-muted-foreground opacity-50")}>
          Next
        </span>
      )}
    </nav>
  );
}
