import { Skeleton } from "@/components/ui/skeleton";

export default function Loading() {
  return (
    <div className="mx-auto grid max-w-6xl gap-10 px-4 py-10 sm:px-6 lg:grid-cols-2">
      <Skeleton className="aspect-square w-full rounded-xl" />
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-3/4" />
        <Skeleton className="h-4 w-1/3" />
        <Skeleton className="h-8 w-1/4" />
        <Skeleton className="h-24 w-full" />
      </div>
    </div>
  );
}
