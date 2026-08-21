import { apiFetch, toQueryString } from "@/lib/api/client";
import { PagedResult, ProductDto } from "@/types/catalog";

export interface GetProductsParams {
  categoryId?: string;
  page?: number;
  pageSize?: number;
}

const DEFAULT_PAGE_SIZE = 12;
// Product/category data only changes via admin tooling out of scope for the storefront,
// so a short revalidate window is enough to keep listings reasonably fresh without
// hammering CatalogService on every request.
const CATALOG_REVALIDATE_SECONDS = 60;

export async function getProducts({
  categoryId,
  page = 1,
  pageSize = DEFAULT_PAGE_SIZE,
}: GetProductsParams = {}): Promise<PagedResult<ProductDto>> {
  const qs = toQueryString({
    categoryId,
    status: "Active",
    page,
    pageSize,
  });

  return apiFetch<PagedResult<ProductDto>>(`/api/products${qs}`, {
    next: { revalidate: CATALOG_REVALIDATE_SECONDS },
  });
}

export async function getProduct(id: string): Promise<ProductDto> {
  return apiFetch<ProductDto>(`/api/products/${id}`, {
    next: { revalidate: CATALOG_REVALIDATE_SECONDS },
  });
}
