"use client";

import { useState } from "react";
import Image from "next/image";
import { cn } from "@/lib/utils";
import { ProductImagePlaceholder } from "@/components/product/ProductImagePlaceholder";
import { ProductImageDto } from "@/types/catalog";

export function ProductGallery({ images, alt }: { images: ProductImageDto[]; alt: string }) {
  const sorted = [...images].sort((a, b) => a.sortOrder - b.sortOrder);
  const [activeId, setActiveId] = useState(sorted[0]?.id);
  const active = sorted.find((image) => image.id === activeId) ?? sorted[0];

  if (!active) {
    return (
      <div className="aspect-square w-full overflow-hidden rounded-xl">
        <ProductImagePlaceholder />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="relative aspect-square w-full overflow-hidden rounded-xl bg-muted">
        <Image
          src={active.url}
          alt={alt}
          fill
          priority
          sizes="(min-width: 1024px) 40vw, 90vw"
          className="object-cover"
        />
      </div>
      {sorted.length > 1 && (
        <div className="flex gap-2">
          {sorted.map((image) => (
            <button
              key={image.id}
              type="button"
              onClick={() => setActiveId(image.id)}
              aria-label={`Show image ${image.sortOrder + 1}`}
              aria-current={image.id === active.id}
              className={cn(
                "relative size-16 shrink-0 overflow-hidden rounded-lg ring-1 ring-border transition-opacity",
                image.id === active.id ? "ring-2 ring-primary" : "opacity-70 hover:opacity-100",
              )}
            >
              <Image src={image.url} alt="" fill sizes="64px" className="object-cover" />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
