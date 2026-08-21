import { apiFetch } from "@/lib/api/client";
import { CategoryDto } from "@/types/catalog";

const CATEGORIES_REVALIDATE_SECONDS = 60;

export async function getCategories(): Promise<CategoryDto[]> {
  return apiFetch<CategoryDto[]>("/api/categories", {
    next: { revalidate: CATEGORIES_REVALIDATE_SECONDS },
  });
}
