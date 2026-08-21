import Link from "next/link";
import { getCategories } from "@/lib/api/categories";
import { getInventory, sumAvailable } from "@/lib/api/inventory";
import { getProducts } from "@/lib/api/products";
import { ProductGrid } from "@/components/product/ProductGrid";

const FEATURED_COUNT = 8;

// Otherwise Next tries to statically prerender this page at `next build` time (it has no
// dynamic API usage - no searchParams, no cookies), which means running its fetch calls inside
// the build environment itself. That's fine for a plain `npm run build` on a machine that also
// has the gateway reachable at NEXT_PUBLIC_API_BASE_URL, but breaks inside `docker build`'s
// isolated build container, which has no route to a live backend at all. Forcing dynamic
// rendering defers these fetches to real request time instead, matching how /products and
// /products/[id] already behave (dynamic by virtue of searchParams/route params).
export const dynamic = "force-dynamic";

export default async function HomePage() {
  const [categories, products] = await Promise.all([getCategories(), getProducts({ pageSize: FEATURED_COUNT })]);

  const items = await Promise.all(
    products.items.map(async (product) => ({
      product,
      available: sumAvailable(await getInventory(product.id)),
    })),
  );

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-12 px-4 py-10 sm:px-6">
      <section className="flex flex-col items-start gap-4 rounded-2xl bg-muted/50 p-8 sm:p-12">
        <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Shop CommerceCore</h1>
        <p className="max-w-xl text-muted-foreground">
          Browse the full catalog and find what you need - new arrivals and everyday essentials, all in
          one place.
        </p>
        <Link
          href="/products"
          className="inline-flex h-9 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/80"
        >
          Shop all products
        </Link>
      </section>

      {categories.length > 0 && (
        <section className="flex flex-col gap-4">
          <h2 className="text-lg font-semibold">Shop by category</h2>
          <div className="flex flex-wrap gap-3">
            {categories.map((category) => (
              <Link
                key={category.id}
                href={`/products?categoryId=${category.id}`}
                className="rounded-full border border-border px-4 py-2 text-sm font-medium transition-colors hover:bg-muted"
              >
                {category.name}
              </Link>
            ))}
          </div>
        </section>
      )}

      <section className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Recently added</h2>
          <Link href="/products" className="text-sm font-medium text-primary hover:underline">
            View all
          </Link>
        </div>
        <ProductGrid items={items} />
      </section>
    </div>
  );
}
