import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // No `output: "standalone"` here (deliberately) — this app deploys to Vercel, which does its
  // own build/packaging and does not expect (or want) Next's standalone server output; setting
  // it breaks the Vercel build with an ENOENT on a trace file standalone mode restructures away.
  // Storefront/Dockerfile (local Docker parity only, not how this actually deploys) is built to
  // work against this same plain `next build` output — see its runtime stage.
  images: {
    // Product image URLs (http://localhost:9000/...) come from CatalogService's own MinIO config
    // (MinioBlobStorageService.GetPublicUrl), not something Storefront picks. next/image's
    // optimizer would fetch that URL server-side to resize it — fine under plain `npm run dev`
    // (server and browser are the same machine), but inside the Storefront Docker container,
    // "localhost:9000" is the container's own loopback, not MinIO, and the fetch fails. Skipping
    // optimization here means the browser fetches the MinIO URL directly instead, which works
    // from both `npm run dev` and Docker alike. Revisit once a real MinIO/CDN hostname exists for
    // non-local environments and server-side optimization is worth reintroducing.
    unoptimized: true,
  },
};

export default nextConfig;
