"use client";

import { useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "@/components/auth/AuthProvider";
import { ApiError } from "@/lib/api/client";
import { Toaster } from "@/components/ui/sonner";

export function Providers({ children }: { children: React.ReactNode }) {
  // Created inside useState, not at module scope - a module-level singleton would leak/share
  // cached data across requests if this ever ran on the server; useState guarantees one instance
  // per component mount (i.e. per browser tab), same as the standard TanStack Query + Next.js
  // App Router setup.
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            // TanStack Query's default retries any error 3 times - fine for a transient network
            // blip, actively wrong for a 4xx: a 404 (e.g. GET /api/orders/{id} for an order that
            // doesn't exist/isn't yours) is deterministic and will keep failing identically, so
            // retrying just delays the real error by several seconds of exponential backoff.
            // Observed directly: retrying a 404 let a later, unrelated 401 become the "final"
            // error shown instead of the real 404 that any retry-blind UI check expects.
            retry: (failureCount, error) =>
              !(error instanceof ApiError && error.status < 500) && failureCount < 2,
          },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        {children}
        <Toaster />
      </AuthProvider>
    </QueryClientProvider>
  );
}
