// Mirrors CatalogService.Api's ProductDto/CategoryDto JSON shapes exactly.
// CatalogService.Application's ProductDto record types Status as `string` (the handler
// calls .ToString() on the domain enum before building the DTO), so it comes over the
// wire as "Draft" | "Active" | "Archived", not a number — verified against a live response.

export type ProductStatus = "Draft" | "Active" | "Archived";

export interface ProductImageDto {
  id: string;
  url: string;
  sortOrder: number;
  isPrimary: boolean;
}

export interface ProductDto {
  id: string;
  name: string;
  description?: string | null;
  sku: string;
  price: number;
  status: ProductStatus;
  categoryId: string;
  createdAt: string;
  updatedAt: string;
  images: ProductImageDto[];
}

export interface CategoryDto {
  id: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
