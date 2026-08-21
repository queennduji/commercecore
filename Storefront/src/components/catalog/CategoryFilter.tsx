import Link from "next/link";
import { cn } from "@/lib/utils";
import { CategoryDto } from "@/types/catalog";

export function CategoryFilter({
  categories,
  activeCategoryId,
}: {
  categories: CategoryDto[];
  activeCategoryId?: string;
}) {
  return (
    <nav aria-label="Filter by category" className="flex flex-wrap gap-2">
      <Link
        href="/products"
        className={cn(
          "rounded-full border px-3 py-1 text-sm transition-colors",
          !activeCategoryId
            ? "border-primary bg-primary text-primary-foreground"
            : "border-border text-muted-foreground hover:text-foreground",
        )}
      >
        All
      </Link>
      {categories.map((category) => (
        <Link
          key={category.id}
          href={`/products?categoryId=${category.id}`}
          className={cn(
            "rounded-full border px-3 py-1 text-sm transition-colors",
            activeCategoryId === category.id
              ? "border-primary bg-primary text-primary-foreground"
              : "border-border text-muted-foreground hover:text-foreground",
          )}
        >
          {category.name}
        </Link>
      ))}
    </nav>
  );
}
