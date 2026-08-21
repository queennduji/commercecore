import type { Metadata } from "next";
import { getCategories } from "@/lib/api/categories";
import { getInventory, sumAvailable } from "@/lib/api/inventory";
import { getProducts } from "@/lib/api/products";
import { CategoryFilter } from "@/components/catalog/CategoryFilter";
import { Pagination } from "@/components/catalog/Pagination";
import { ProductGrid } from "@/components/product/ProductGrid";

export const metadata: Metadata = { title: "Products" };

export default async function ProductsPage({
  searchParams,
}: {
  searchParams: Promise<{ categoryId?: string; page?: string }>;
}) {
  const { categoryId, page: pageParam } = await searchParams;
  const page = Math.max(1, Number(pageParam) || 1);

  const [categories, products] = await Promise.all([
    getCategories(),
    getProducts({ categoryId, page }),
  ]);

  const items = await Promise.all(
    products.items.map(async (product) => ({
      product,
      available: sumAvailable(await getInventory(product.id)),
    })),
  );

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-10 sm:px-6">
      <h1 className="text-2xl font-semibold tracking-tight">Products</h1>
      <CategoryFilter categories={categories} activeCategoryId={categoryId} />
      <ProductGrid items={items} />
      <Pagination page={products.page} pageSize={products.pageSize} totalCount={products.totalCount} categoryId={categoryId} />
    </div>
  );
}
