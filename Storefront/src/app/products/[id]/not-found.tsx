import Link from "next/link";

export default function ProductNotFound() {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-4 px-4 py-24 text-center">
      <h1 className="text-xl font-semibold">Product not found</h1>
      <p className="text-sm text-muted-foreground">
        This product may have been removed or is no longer available.
      </p>
      <Link
        href="/products"
        className="inline-flex h-9 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/80"
      >
        Browse products
      </Link>
    </div>
  );
}
